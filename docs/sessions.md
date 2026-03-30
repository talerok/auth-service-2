# Session Management

Серверные сессии привязаны к OIDC-токенам через claim `sid`. Каждая успешная аутентификация создаёт запись `UserSession` в БД, а `sid` включается в access token и id token. Это позволяет отзывать доступ на уровне отдельной сессии или пользователя.

## Модель данных

### UserSession

| Поле | Тип | Описание |
|------|-----|----------|
| `Id` | `Guid` | PK, генерируется при создании |
| `UserId` | `Guid` | FK → `users.Id` (CASCADE) |
| `ApplicationId` | `Guid?` | FK → `applications.Id` (SET NULL) |
| `IpAddress` | `string(45)` | IP клиента на момент аутентификации |
| `UserAgent` | `string(500)` | User-Agent клиента (обрезается до 500 символов) |
| `AuthMethod` | `string(32)` | Метод аутентификации: `pwd`, `pwd+otp`, `fed`, `ldap` |
| `IsRevoked` | `bool` | Флаг отзыва |
| `CreatedAt` | `DateTime` | Время создания |
| `ExpiresAt` | `DateTime` | `CreatedAt + RefreshTokenLifetimeDays` |
| `LastActivityAt` | `DateTime` | Обновляется при refresh token exchange |
| `RevokedAt` | `DateTime?` | Время отзыва |
| `RevokedReason` | `string(100)?` | Причина отзыва: `logout`, `admin`, etc. |

`IsActive` = `!IsRevoked && ExpiresAt > UtcNow`

## Жизненный цикл

### 1. Создание сессии

Сессия создаётся при успешной аутентификации в одном из grant handler'ов:

| Grant | AuthMethod | Handler |
|-------|------------|---------|
| Password | `pwd` | `ValidateCredentialsForLoginCommandHandler` |
| MFA OTP | `pwd+otp` | `HandleMfaOtpGrantCommandHandler` |
| JWT Bearer (federated) | `fed` | `HandleJwtBearerGrantCommandHandler` |
| LDAP | `ldap` | `HandleLdapGrantCommandHandler` |

Каждый handler:
1. Валидирует credentials
2. Проверяет 2FA — если требуется, возвращает `MfaRequired` без создания сессии
3. Извлекает IP и User-Agent из `IHttpContextAccessor`
4. Отправляет `CreateSessionCommand(userId, clientId, authMethod, ip, ua)`
5. `CreateSessionCommandHandler` резолвит `ApplicationId` из `clientId` (lookup `applications.ClientId → applications.Id`), вызывает `UserSession.Create()`, сохраняет в БД, возвращает `sessionId`

При 2FA аутентификация проходит в два шага: password grant → `MfaRequired` → MFA OTP grant → сессия создаётся.

### 2. Привязка к токенам (claim `sid`)

После создания сессии handler вызывает `BuildPrincipalQuery` с `sessionId`. В `OidcPrincipalFactory` claim `sid` добавляется в identity:

```csharp
if (sessionId.HasValue)
    identity.SetClaim("sid", sessionId.Value.ToString());
```

Destinations для `sid` — `AccessToken` и `IdentityToken`. OpenIddict включает `sid` в оба токена при подписании.

Client credentials grant **не создаёт сессию** — service account'ы работают без `sid`.

### 3. Обновление при refresh

При обмене refresh token (`POST /connect/token`, `grant_type=refresh_token`) `TokenController.HandleSubjectGrant`:

1. OpenIddict валидирует refresh token (подпись, срок, rotation)
2. Из principal извлекаются `sid` и `sub`
3. `TouchSessionCommand(sessionId, userId)`:
   - Загружает сессию из БД
   - Проверяет `IsActive` (`!IsRevoked && ExpiresAt > UtcNow`)
   - Обновляет `LastActivityAt`
   - Если сессия отозвана — бросает `AuthException` → клиент получает `invalid_grant`
4. `BuildPrincipalQuery` с тем же `sessionId` → новый access token с тем же `sid`

Сессия **не пересоздаётся** при refresh — один login = одна сессия на весь срок жизни refresh token.

### 4. Отзыв

#### Отзыв одной сессии

- **Пользователь (logout):** `POST /connect/logout` → `RevokeOwnSessionCommand` → `session.Revoke("logout")`
- **Пользователь (UI):** `DELETE /api/account/sessions/{id}` → `RevokeOwnSessionCommand` (проверяет ownership)
- **Админ:** `DELETE /api/users/{userId}/sessions/{id}` → `RevokeSessionCommand` → `session.Revoke("admin")`

#### Отзыв всех сессий пользователя

`DELETE /api/users/{userId}/sessions` → `RevokeUserSessionsCommand`:
1. Отзывает все активные сессии пользователя

При отзыве сессии access token продолжает работать до истечения (по умолчанию 15 мин). Refresh token блокируется через `TouchSession`.

## Валидация токенов

### ValidateSessionOnIntrospection

Срабатывает при вызове `POST /connect/introspect` (resource servers). Проверяет:

- Прямой SQL-запрос: `SELECT (!IsRevoked AND ExpiresAt > UtcNow) FROM user_sessions WHERE Id = @sid`

Если сессия неактивна — токен возвращается как `active: false`.

### Матрица инвалидации

| Действие | Access token | Refresh grant | Introspection |
|----------|--------------|---------------|---------------|
| `session.Revoke()` | Работает до истечения | Блокирован (TouchSession) | `active: false` |

## API

### Пользовательские endpoints

| Метод | Путь | Описание |
|-------|------|----------|
| `GET` | `/api/account/sessions` | Список своих сессий (текущая помечена `isCurrent`) |
| `DELETE` | `/api/account/sessions/{id}` | Отзыв своей сессии |
| `DELETE` | `/api/account/sessions` | Отзыв всех своих сессий |

### Административные endpoints

| Метод | Путь | Permission | Описание |
|-------|------|------------|----------|
| `GET` | `/api/users/{userId}/sessions` | `system.sessions.view` | Сессии пользователя |
| `DELETE` | `/api/users/{userId}/sessions/{id}` | `system.sessions.revoke` | Отзыв сессии |
| `DELETE` | `/api/users/{userId}/sessions` | `system.sessions.revoke-all` | Отзыв всех сессий |

### OIDC endpoints (OpenIddict)

| Метод | Путь | Роль в сессиях |
|-------|------|----------------|
| `POST` | `/connect/token` | Создание сессии (password/mfa/fed/ldap grant), обновление `LastActivityAt` (refresh grant) |
| `POST` | `/connect/logout` | Отзыв текущей сессии |
| `POST` | `/connect/introspect` | Валидация сессии для resource servers |

## Конфигурация

```
Integration__Oidc__AccessTokenLifetimeMinutes=15     # Время жизни access token
Integration__Oidc__RefreshTokenLifetimeDays=7         # Время жизни refresh token = время жизни сессии
```

Время жизни сессии (`ExpiresAt`) совпадает с `RefreshTokenLifetimeDays`. После истечения refresh token сессия автоматически становится неактивной (`ExpiresAt > UtcNow` = false).

## Индексы

| Индекс | Колонки | Назначение |
|--------|---------|------------|
| `IX_user_sessions_UserId` | `UserId` | Выборка сессий пользователя |
| `IX_user_sessions_UserId_Active` | `UserId, ExpiresAt` WHERE `IsRevoked = false` | Быстрый поиск активных сессий |
| `IX_user_sessions_ApplicationId` | `ApplicationId` | Фильтрация по приложению |
| `IX_user_sessions_CreatedAt` | `CreatedAt DESC` | Сортировка в UI |

## Аудит

Все операции с сессиями логируются в audit trail:

| Действие | `AuditAction` | `EntityType` |
|----------|---------------|--------------|
| Создание сессии | `CreateSession` | `Session` |
| Отзыв одной сессии | `RevokeSession` | `Session` |
| Отзыв всех сессий | `RevokeAllSessions` | `User` |

## Диаграмма

```
Аутентификация (pwd/mfa/fed/ldap)
  → CreateSessionCommand → INSERT user_sessions → sessionId
  → BuildPrincipalQuery(sessionId) → sid claim в principal
  → OpenIddict подписывает → JWT {sub, sid, iat, ...}
  → Клиент получает access_token + refresh_token

Refresh token exchange:
  → OpenIddict валидирует refresh token
  → TouchSession(sid, sub): проверка IsActive, обновление LastActivityAt
  → BuildPrincipalQuery(тот же sid) → новый access token
  → Rotation: старый refresh token инвалидирован, новый выдан

Introspection (resource server):
  → ValidateSessionOnIntrospection:
    SELECT IsActive FROM user_sessions WHERE Id = sid

Отзыв:
  → session.Revoke(reason) → IsRevoked = true
  → Refresh token блокируется через TouchSession
  → Access token живёт до истечения (15 мин)
```
