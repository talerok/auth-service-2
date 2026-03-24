# Потоки аутентификации

## Эндпоинты

| Эндпоинт | Метод | Описание |
|---|---|---|
| `/connect/authorize` | GET | Точка входа Authorization Code Flow |
| `/connect/login` | POST | Аутентификация по логину/паролю |
| `/connect/mfa/verify` | POST | Проверка OTP-кода (MFA) |
| `/connect/authorize/consent` | POST | Согласие пользователя на OAuth-скоупы |
| `/connect/token` | POST | Выдача токенов (все grant types) |
| `/connect/logout` | GET/POST | Завершение сессии |
| `/connect/userinfo` | GET/POST | Получение claims пользователя |
| `/connect/client-info` | GET | Метаданные клиентского приложения |
| `/api/account/password/forced-change` | POST | Принудительная смена пароля |
| `/api/account/password-requirements` | GET | Правила парольной политики |

## Authorization Code Flow

Стандартный OAuth 2.0 / OpenID Connect flow для веб-приложений.

```mermaid
sequenceDiagram
    participant U as Пользователь
    participant C as Клиентское приложение
    participant A as Auth Server
    participant DB as БД

    U->>C: Нажимает "Войти"
    C->>A: GET /connect/authorize<br/>?client_id=app1<br/>&redirect_uri=https://...<br/>&response_type=code<br/>&scope=openid profile

    A->>A: Проверка cookie Identity.External
    alt Нет cookie
        A-->>U: 302 → /auth/login.html?returnUrl=...
        Note over U,A: См. подпоток "Логин" ниже
        U->>A: GET /connect/authorize (повтор с cookie)
    end

    A->>DB: Валидация клиента, проверка существующей авторизации
    alt Требуется согласие (ConsentType.Explicit)
        A-->>U: 302 → /auth/consent.html?client_id=...&scope=...
        Note over U,A: См. подпоток "Согласие" ниже
        U->>A: GET /connect/authorize (повтор после согласия)
    end

    A->>DB: Формирование principal, создание authorization code
    A-->>C: 302 → redirect_uri?code=abc123&state=xyz

    C->>A: POST /connect/token<br/>grant_type=authorization_code<br/>code=abc123&client_secret=...
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

```mermaid
sequenceDiagram
    participant C as Клиентское приложение
    participant A as Auth Server

    C->>A: POST /connect/token<br/>grant_type=refresh_token<br/>refresh_token=...&client_id=app1<br/>client_secret=...
    A-->>C: 200 { access_token, refresh_token }

    Note over A: TTL access token: 15 мин<br/>TTL refresh token: 7 дней
```

## Token Exchange Grant

Федерация с внешними источниками идентификации (например, Google).

```mermaid
sequenceDiagram
    participant C as Клиентское приложение
    participant A as Auth Server
    participant DB as БД

    C->>A: POST /connect/token<br/>grant_type=urn:custom:token_exchange<br/>identity_source=google<br/>username=user@gmail.com<br/>token=google-jwt&scope=openid profile

    A->>DB: Валидация конфигурации identity source
    A->>A: Верификация внешнего токена
    A->>DB: Поиск/создание связанного пользователя
    A-->>C: 200 { access_token, refresh_token, id_token }
```

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
        → GET /connect/authorize    (cookie установлен, требуется согласие)
          → /auth/consent.html      (одобрение скоупов)
            → GET /connect/authorize (выдача кода)
              → redirect_uri?code=... (возврат к клиенту)
                → POST /connect/token (обмен кода на токены)
```

## Скоупы

| Скоуп | Описание | Claims |
|---|---|---|
| `openid` | Обязательный. Идентификация пользователя | `sub` |
| `profile` | Профиль пользователя | `name`, `preferred_username` |
| `email` | Адрес электронной почты | `email` |
| `phone` | Номер телефона | `phone_number` |
| `ws:*` | Все доступные рабочие пространства | `ws:{code}` (base64-encoded permission masks) |
| `ws:{code}` | Конкретное рабочее пространство | `ws:{code}` (base64-encoded permission masks) |
| `offline_access` | Выдача refresh token | - |

## Время жизни токенов

| Токен | TTL |
|---|---|
| Access token | 15 мин |
| Refresh token | 7 дней |
| Cookie Identity.External | 5 мин |
| MFA OTP challenge | 5 мин (3 мин для высокого риска) |
| Password change challenge | 15 мин |
