# Identity Federation

Аутентификация пользователей через внешние OIDC-провайдеры (Keycloak, Okta, Azure AD и др.).

## Основные сущности

- **IdentitySource** — внешний провайдер (name, type, oidcConfig). Soft delete.
- **IdentitySourceOidcConfig** — параметры OIDC: `authority`, `clientId`, `clientSecret` (опционально).
- **IdentitySourceLink** — связка (userId, identitySourceId, externalIdentity). Уникальность по `(identitySourceId, externalIdentity)`.

## Таблицы

### `identity_sources`

| Колонка        | Тип          | Описание                        |
| -------------- | ------------ | ------------------------------- |
| `id`           | uuid         | PK                              |
| `name`         | varchar(200) | Уникальное имя (partial index)  |
| `display_name` | varchar(200) | Отображаемое имя                |
| `type`         | varchar(50)  | Тип провайдера (`oidc`, `ldap`) |
| `is_enabled`   | boolean      | Активен ли провайдер            |
| `deleted_at`   | timestamptz  | Soft delete                     |

### `identity_source_oidc_configs`

| Колонка              | Тип          | Описание                                  |
| -------------------- | ------------ | ----------------------------------------- |
| `id`                 | uuid         | PK                                        |
| `identity_source_id` | uuid         | FK → identity_sources(id) CASCADE, UNIQUE |
| `authority`          | varchar(500) | OIDC issuer URL                           |
| `client_id`          | varchar(200) | Client ID для валидации audience          |
| `client_secret`      | varchar(500) | Client secret (опционально)               |

### `identity_source_links`

| Колонка              | Тип          | Описание                          |
| -------------------- | ------------ | --------------------------------- |
| `id`                 | uuid         | PK                                |
| `user_id`            | uuid         | FK → users(id) CASCADE            |
| `identity_source_id` | uuid         | FK → identity_sources(id) CASCADE |
| `external_identity`  | varchar(500) | Sub из внешнего токена            |
| `created_at`         | timestamptz  |                                   |

Индекс: `(identity_source_id, external_identity)` UNIQUE.

## API

### CRUD — `/api/identity-sources`

Стандартный CRUD. Требует полномочия `identity-sources.*` в workspace `default`.

| Метод  | Путь                                          | Описание               |
| ------ | --------------------------------------------- | ---------------------- |
| GET    | `/api/identity-sources`                       | Список источников      |
| GET    | `/api/identity-sources/{id}`                  | Детали источника       |
| POST   | `/api/identity-sources`                       | Создание               |
| PUT    | `/api/identity-sources/{id}`                  | Обновление             |
| DELETE | `/api/identity-sources/{id}`                  | Удаление (soft delete) |
| GET    | `/api/identity-sources/{id}/links`            | Список связей          |
| POST   | `/api/identity-sources/{id}/links`            | Создание связи         |
| DELETE | `/api/identity-sources/{sourceId}/links/{id}` | Удаление связи         |

### Token Exchange — `/connect/token`

Аутентификация через внешний токен реализована как custom OpenIddict grant type.

**Запрос** (form-encoded на `/connect/token`):

```
grant_type=urn:custom:token_exchange
identity_source=keycloak
token=<внешний_jwt>
scope=openid profile ws
client_id=frontend-app
```

**Ответ** — стандартный OpenIddict token response (access_token + refresh_token).

Возможны промежуточные ответы:

- `mfa_required` — требуется 2FA, возвращается `mfa_token` и `mfa_channel`
- `password_change_required` — требуется смена пароля, возвращается `challenge_id`

## Поток аутентификации

```
Client                    Auth Service                   External IdP
  |                            |                              |
  |  POST /connect/token       |                              |
  |  grant_type=token_exchange |                              |
  |  identity_source=keycloak  |                              |
  |  token=<jwt>               |                              |
  |--------------------------->|                              |
  |                            |  GET /.well-known/openid-    |
  |                            |  configuration               |
  |                            |----------------------------->|
  |                            |  JWKS + issuer               |
  |                            |<-----------------------------|
  |                            |                              |
  |                            |  Validate JWT signature      |
  |                            |  Verify issuer + audience    |
  |                            |  Extract sub claim           |
  |                            |                              |
  |                            |  Find link by (source, sub)  |
  |                            |  Check user is active        |
  |                            |  Check 2FA / password change |
  |                            |  Build ClaimsPrincipal       |
  |                            |                              |
  |  { access_token, ... }     |                              |
  |<---------------------------|                              |
```

## Валидация внешнего токена

`OidcTokenValidator` использует стандартный OIDC discovery (`/.well-known/openid-configuration`) для получения JWKS.

- Конфигурация кешируется в `ConcurrentDictionary<authority, ConfigurationManager>`
- Проверяется подпись, issuer, audience (clientId), срок действия
- Возвращается claim `sub` из токена

## Коды ошибок

| Code                                  | HTTP | Описание                              |
| ------------------------------------- | ---- | ------------------------------------- |
| `AUTH_IDENTITY_SOURCE_NOT_FOUND`      | 404  | Источник не найден                    |
| `AUTH_IDENTITY_SOURCE_DISABLED`       | 400  | Источник отключен                     |
| `AUTH_IDENTITY_SOURCE_TOKEN_INVALID`  | 401  | Внешний токен невалиден               |
| `AUTH_IDENTITY_SOURCE_LINK_NOT_FOUND` | 401  | Нет связки для данного sub            |
| `AUTH_IDENTITY_SOURCE_USER_INACTIVE`  | 401  | Привязанный пользователь неактивен    |
| `AUTH_IDENTITY_SOURCE_DUPLICATE_LINK` | 400  | Дубликат связки                       |
| `AUTH_IDENTITY_SOURCE_TYPE_MISMATCH`  | 400  | Тип источника не совпадает с запросом |
