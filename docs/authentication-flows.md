# Потоки аутентификации

## Эндпоинты

| Эндпоинт | Метод | Описание |
|---|---|---|
| `/connect/authorize` | GET/POST | Точка входа Authorization Code Flow |
| `/connect/login` | POST | Аутентификация по логину/паролю |
| `/connect/mfa/verify` | POST | Проверка OTP-кода (MFA) |
| `/connect/authorize/consent` | POST | Согласие пользователя на OAuth-скоупы |
| `/connect/token` | POST | Выдача токенов (все grant types) |
| `/connect/logout` | GET/POST | Завершение сессии |
| `/connect/userinfo` | GET/POST | Получение claims пользователя |
| `/connect/revocation` | POST | Отзыв токена |
| `/connect/introspect` | POST | Интроспекция токена |
| `/connect/client-info` | GET | Метаданные клиентского приложения |
| `/api/account/password-requirements` | GET | Правила парольной политики |
| `/api/account/password/forced-change` | POST | Принудительная смена пароля |
| `/api/account/2fa/enable` | POST | Включение 2FA |
| `/api/account/2fa/confirm` | POST | Подтверждение активации 2FA |
| `/api/account/2fa/disable` | POST | Отключение 2FA |
| `/api/account/verify-email/send` | POST | Отправка верификации email |
| `/api/account/verify-email/confirm` | POST | Подтверждение верификации email |
| `/api/account/verify-phone/send` | POST | Отправка верификации телефона |
| `/api/account/verify-phone/confirm` | POST | Подтверждение верификации телефона |

## Authorization Code Flow

Стандартный OAuth 2.0 / OpenID Connect flow для веб-приложений. **PKCE обязателен.**

```mermaid
sequenceDiagram
    participant U as Пользователь
    participant C as Клиентское приложение
    participant A as Auth Server
    participant DB as БД

    U->>C: Нажимает "Войти"
    C->>A: GET /connect/authorize<br/>?client_id=app1<br/>&redirect_uri=https://...<br/>&response_type=code<br/>&scope=openid profile<br/>&code_challenge=...&code_challenge_method=S256

    A->>A: Проверка cookie Identity.External
    alt Нет cookie
        A-->>U: 302 → /auth/login.html?returnUrl=...
        Note over U,A: См. подпоток "Логин" ниже
        U->>A: GET /connect/authorize (повтор с cookie)
    end

    A->>DB: Валидация клиента, проверка верификации email/phone
    alt Требуется верификация (per-application)
        A-->>C: error: access_denied, "email/phone verification is required"
    end

    alt Требуется согласие (ConsentType.Explicit)
        A-->>U: 302 → /auth/consent.html?client_id=...&scope=...
        Note over U,A: См. подпоток "Согласие" ниже
        U->>A: GET /connect/authorize (повтор после согласия)
    end

    A->>DB: Формирование principal, создание authorization code
    A-->>C: 302 → redirect_uri?code=abc123&state=xyz

    C->>A: POST /connect/token<br/>grant_type=authorization_code<br/>code=abc123&client_secret=...<br/>&code_verifier=...
    A-->>C: { access_token, refresh_token, id_token }
```

### Логин

```mermaid
sequenceDiagram
    participant U as Пользователь
    participant P as login.html
    participant A as Auth Server

    U->>P: Вводит логин и пароль
    P->>A: POST /connect/login<br/>{ username, password, returnUrl }

    alt Успех
        A->>A: Устанавливает cookie Identity.External (TTL 5 мин)
        A-->>P: 200 { redirect_url }
        P-->>U: Редирект → /connect/authorize
    end

    alt Требуется MFA
        A-->>P: 400 { error: "mfa_required",<br/>mfa_token, mfa_channel }
        P-->>U: Редирект → /auth/mfa.html
    end

    alt Требуется смена пароля
        A-->>P: 400 { error: "password_change_required",<br/>challenge_id }
        P-->>U: Редирект → /auth/password-change.html
    end

    alt Неверные учётные данные
        A-->>P: 400 { error: "invalid_credentials" }
        P-->>U: Показывает ошибку
    end
```

### Проверка MFA

```mermaid
sequenceDiagram
    participant U as Пользователь
    participant P as mfa.html
    participant A as Auth Server

    Note over U,A: OTP уже отправлен на email/SMS при логине

    U->>P: Вводит 6-значный OTP
    P->>A: POST /connect/mfa/verify<br/>{ mfa_token, mfa_channel, otp, return_url }

    alt Верный OTP
        A->>A: Устанавливает cookie Identity.External (TTL 5 мин)
        A-->>P: 200 { redirect_url }
        P-->>U: Редирект → /connect/authorize
    end

    alt Неверный OTP
        A-->>P: 400 { error: "invalid_otp" }
        P-->>U: Показывает ошибку
    end

    Note over A: Макс. 5 попыток, TTL 5 мин (3 мин для высокого риска)
```

### Принудительная смена пароля

```mermaid
sequenceDiagram
    participant U as Пользователь
    participant P as password-change.html
    participant A as Auth Server

    P->>A: GET /api/account/password-requirements
    A-->>P: { minLength, requireUppercase, ... }

    U->>P: Вводит новый пароль
    P->>P: Валидация на клиенте по правилам
    P->>A: POST /api/account/password/forced-change<br/>{ challengeId, newPassword }

    alt Успех
        A-->>P: 204 No Content
        P-->>U: Редирект → /auth/login.html?returnUrl=...
        Note over U: Пользователь входит заново с новым паролем
    end

    alt Ошибка
        A-->>P: 400 { detail: "..." }
        P-->>U: Показывает ошибку
    end

    Note over A: TTL challenge 15 мин, одноразовый
```

### Согласие (Consent)

```mermaid
sequenceDiagram
    participant U as Пользователь
    participant P as consent.html
    participant A as Auth Server

    P->>A: GET /connect/client-info?client_id=app1
    A-->>P: { name, logoUrl, homepageUrl }

    U->>P: Нажимает "Разрешить" или "Отклонить"
    P->>A: POST /connect/authorize/consent<br/>{ clientId, scopes, approved, returnUrl }

    alt Разрешено
        A->>A: Создание/переиспользование OpenIddict Authorization
        A-->>P: 200 { redirect_url }
        P-->>U: Редирект → /connect/authorize
    end

    alt Отклонено
        A-->>P: 200 { error: "access_denied" }
        P-->>U: Показывает ошибку / редирект к клиенту с ошибкой
    end
```

## Password Grant

Прямая выдача токенов без браузерных редиректов. Только для доверенных first-party приложений.

```mermaid
sequenceDiagram
    participant C as Клиентское приложение
    participant A as Auth Server
    participant DB as БД

    C->>A: POST /connect/token<br/>grant_type=password<br/>username=john&password=secret<br/>scope=openid profile

    A->>DB: Валидация учётных данных

    alt Успех
        A->>A: Формирование principal
        A-->>C: 200 { access_token, refresh_token, id_token }
    end

    alt Требуется MFA
        A-->>C: 400 { error: "mfa_required",<br/>mfa_token, mfa_channel }
        Note over C: Используйте MFA OTP grant для завершения
    end

    alt Требуется смена пароля
        A-->>C: 400 { error: "password_change_required",<br/>challenge_id }
    end

    alt Неверные учётные данные
        A-->>C: 400 { error: "invalid_grant" }
    end
```

### Password Grant + MFA (двухшаговый)

```mermaid
sequenceDiagram
    participant C as Клиентское приложение
    participant A as Auth Server

    C->>A: POST /connect/token<br/>grant_type=password<br/>username=john&password=secret
    A-->>C: 400 { error: "mfa_required",<br/>mfa_token: "...", mfa_channel: "email" }

    Note over C: Пользователь получает OTP на email/SMS

    C->>A: POST /connect/token<br/>grant_type=urn:custom:mfa_otp<br/>mfa_token=...&mfa_channel=email<br/>otp=123456&scope=openid profile
    A-->>C: 200 { access_token, refresh_token, id_token }
```

## Client Credentials Grant

Аутентификация сервис-сервис без пользовательского контекста. Используется сервисными аккаунтами.

```mermaid
sequenceDiagram
    participant S as Сервис
    participant A as Auth Server

    S->>A: POST /connect/token<br/>grant_type=client_credentials<br/>client_id=sa-xxx&client_secret=...<br/>scope=ws:system
    A-->>S: 200 { access_token }

    Note over S: Без refresh_token и id_token (нет пользовательского контекста)
```

Особенности SA:
- При создании SA регистрируется OpenIddict application с `ws:*` scope permission
- При назначении workspace (SetWorkspaces) scope permissions синхронизируются: `ws:*` + `ws:{code}` для каждого назначенного workspace
- `audiences` (aud claim) берутся из поля SA, а не из таблицы applications
- `access_token_lifetime_minutes` позволяет задать индивидуальный TTL токена

## Refresh Token Grant

Refresh token rotation: при каждом обмене выдаётся новый refresh token, старый отзывается.

```mermaid
sequenceDiagram
    participant C as Клиентское приложение
    participant A as Auth Server

    C->>A: POST /connect/token<br/>grant_type=refresh_token<br/>refresh_token=RT1&client_id=app1<br/>client_secret=...
    A-->>C: 200 { access_token: AT2, refresh_token: RT2 }

    Note over A: RT1 отозван. RT2 — новый.<br/>TTL access token: 15 мин<br/>TTL refresh token: 7 дней
```

### Token rotation

- При обмене refresh token всегда выдаётся новый, старый помечается как redeemed
- **Reuse leeway** (default 30 сек) — окно, в течение которого повторное использование redeemed-токена допустимо (для race conditions)
- **Replay detection** — использование redeemed-токена за пределами leeway отзывает всю token family (все потомки)

## JWT Bearer Grant (OIDC Federation)

Федерация с внешними OIDC-провайдерами через `urn:ietf:params:oauth:grant-type:jwt-bearer`.

```mermaid
sequenceDiagram
    participant C as Клиентское приложение
    participant A as Auth Server
    participant IdP as OIDC Provider

    C->>A: POST /connect/token<br/>grant_type=urn:ietf:params:oauth:grant-type:jwt-bearer<br/>assertion=<external_jwt><br/>scope=openid profile&client_id=app1

    A->>A: Извлечение issuer из JWT
    A->>IdP: OIDC Discovery + JWKS
    A->>A: Валидация подписи, issuer, audience, exp
    A->>A: Поиск identity_source_link по (source, sub)
    A-->>C: 200 { access_token, refresh_token, id_token }
```

Может вернуть `mfa_required` или `password_change_required`.

## LDAP Grant

Аутентификация через LDAP-каталог. Custom grant type `urn:custom:ldap`.

```mermaid
sequenceDiagram
    participant C as Клиентское приложение
    participant A as Auth Server
    participant L as LDAP Server

    C->>A: POST /connect/token<br/>grant_type=urn:custom:ldap<br/>identity_source=active-directory<br/>username=john.doe&password=secret<br/>scope=openid profile&client_id=app1

    A->>L: LDAP BIND (service account)
    A->>L: Search by filter (uid=john.doe)
    A->>L: LDAP BIND (user DN + password)
    A->>A: Поиск identity_source_link по (source, username)
    A-->>C: 200 { access_token, refresh_token, id_token }
```

Может вернуть `mfa_required` или `password_change_required`.

## Email/Phone Verification

Per-application verification: приложение может требовать подтверждённый email и/или телефон (`require_email_verified`, `require_phone_verified`). Проверка выполняется в Authorization Code Flow при `/connect/authorize`.

### Отправка верификации

```mermaid
sequenceDiagram
    participant C as Клиент
    participant A as Auth Server

    C->>A: POST /api/account/verify-email/send (Authorized)
    A-->>C: 200 { challengeId }
    Note over A: OTP отправляется email/SMS через фоновый worker
```

### Подтверждение верификации

```mermaid
sequenceDiagram
    participant U as Пользователь
    participant P as verify.html
    participant A as Auth Server

    Note over U: Переходит по ссылке из email/SMS<br/>?type=email&challengeId=...&otp=...

    P->>A: POST /api/account/verify-email/confirm (AllowAnonymous)<br/>{ challengeId, otp }

    alt Успех
        A->>A: user.EmailVerified = true
        A-->>P: 204 No Content
    end

    alt Ошибка
        A-->>P: 400 { error }
    end
```

Аналогично для телефона: `verify-phone/send` и `verify-phone/confirm`.

## Logout

```mermaid
sequenceDiagram
    participant U as Пользователь
    participant C as Клиентское приложение
    participant A as Auth Server

    U->>C: Нажимает "Выйти"
    C->>A: GET /connect/logout
    A->>A: Удаление cookie Identity.External
    A->>A: Завершение сессии OpenIddict
    A-->>U: 302 → /
```

## Полный поток (максимальный сценарий)

Максимальная цепочка редиректов, когда требуются все шаги:

```
Клиентское приложение
  → GET /connect/authorize          (нет cookie)
    → /auth/login.html              (ввод учётных данных)
      → /auth/mfa.html              (ввод OTP)
        → GET /connect/authorize    (cookie установлен, проверка верификации)
          → error если email/phone не верифицирован
          → /auth/consent.html      (одобрение скоупов)
            → GET /connect/authorize (выдача кода)
              → redirect_uri?code=... (возврат к клиенту)
                → POST /connect/token (обмен кода на токены)
```

## Скоупы

| Скоуп | Описание | Claims |
|---|---|---|
| `openid` | Обязательный. Идентификация пользователя | `sub` |
| `profile` | Профиль пользователя | `name`, `preferred_username`, `locale` |
| `email` | Адрес электронной почты | `email`, `email_verified` |
| `phone` | Номер телефона | `phone_number`, `phone_number_verified` |
| `ws:*` | Все доступные рабочие пространства | `ws:{code}` (JSON с base64-encoded permission masks) |
| `ws:{code}` | Конкретное рабочее пространство | `ws:{code}` (JSON с base64-encoded permission masks) |
| `offline_access` | Выдача refresh token | - |

## Claims и destinations

| Claim | Destination |
|---|---|
| `sub` | access_token, id_token |
| `name` | access_token, id_token |
| `preferred_username` | id_token |
| `locale` | id_token |
| `email` | id_token |
| `email_verified` | id_token |
| `phone_number` | id_token |
| `phone_number_verified` | id_token |
| `auth_time` | id_token |
| `amr` | id_token |
| `pwd_exp` | access_token, id_token |
| `ws:{code}` | access_token |

`pwd_exp` — unix timestamp истечения пароля. Присутствует только если настроен password expiration (глобально или per-user).

`amr` (authentication method reference): `pwd` (пароль / LDAP), `otp` (MFA), `fed` (OIDC federation).

## Время жизни токенов

| Токен | TTL | Переопределение |
|---|---|---|
| Access token | 15 мин | per-application `access_token_lifetime_minutes` |
| Refresh token | 7 дней | per-application `refresh_token_lifetime_minutes` |
| Cookie Identity.External | 5 мин | — |
| MFA OTP challenge | 5 мин (3 мин для высокого риска) | — |
| Password change challenge | 15 мин | — |
