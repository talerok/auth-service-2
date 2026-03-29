# Identity Federation

Аутентификация пользователей через внешние OIDC-провайдеры (Keycloak, Okta, Azure AD и др.) и LDAP-каталоги (Active Directory, OpenLDAP и др.).

## Основные сущности

- **IdentitySource** — внешний провайдер (name, code, type). Поддерживает `oidc` и `ldap`. Soft delete.
- **IdentitySourceOidcConfig** — параметры OIDC: `authority`, `clientId`, `clientSecret` (шифруется at-rest).
- **IdentitySourceLdapConfig** — параметры LDAP: `host`, `port`, `baseDn`, `bindDn`, `bindPassword` (шифруется at-rest), `useSsl`, `searchFilter`.
- **IdentitySourceLink** — связка (userId, identitySourceId, externalIdentity). Уникальность по `(identitySourceId, externalIdentity)`.

## Таблицы

### `identity_sources`

| Колонка        | Тип          | Nullable | Описание                        |
| -------------- | ------------ | -------- | ------------------------------- |
| `id`           | uuid         | NO       | PK                              |
| `name`         | varchar(120) | NO       | Уникальное имя (partial index)  |
| `code`         | varchar(64)  | NO       | Уникальный код (partial index)  |
| `display_name` | varchar(200) | NO       | Отображаемое имя                |
| `type`         | varchar(16)  | NO       | Тип провайдера (`oidc`, `ldap`) |
| `is_enabled`   | boolean      | NO       | Активен ли провайдер            |
| `created_at`   | timestamptz  | NO       |                                 |
| `updated_at`   | timestamptz  | NO       |                                 |
| `deleted_at`   | timestamptz  | YES      | Soft delete                     |

Индексы:

- `IX_identity_sources_Name` UNIQUE WHERE `"DeletedAt" IS NULL`
- `IX_identity_sources_Code` UNIQUE WHERE `"DeletedAt" IS NULL`

### `identity_source_oidc_configs`

| Колонка              | Тип          | Nullable | Описание                                  |
| -------------------- | ------------ | -------- | ----------------------------------------- |
| `id`                 | uuid         | NO       | PK                                        |
| `identity_source_id` | uuid         | NO       | FK → identity_sources(id) CASCADE, UNIQUE |
| `authority`          | varchar(500) | NO       | OIDC issuer URL                           |
| `client_id`          | varchar(200) | NO       | Client ID для валидации audience          |
| `client_secret`      | varchar(500) | YES      | Client secret (шифруется at-rest)         |

### `identity_source_ldap_configs`

| Колонка              | Тип          | Nullable | Описание                                 |
| -------------------- | ------------ | -------- | ---------------------------------------- |
| `id`                 | uuid         | NO       | PK                                       |
| `identity_source_id` | uuid         | NO       | FK → identity_sources(id) CASCADE, UNIQUE|
| `host`               | varchar(500) | NO       | LDAP-сервер                              |
| `port`               | integer      | NO       | Порт (DEFAULT 389)                       |
| `base_dn`            | varchar(500) | NO       | Base DN для поиска                       |
| `bind_dn`            | varchar(500) | NO       | DN сервисного аккаунта                   |
| `bind_password`      | varchar(500) | YES      | Пароль (шифруется at-rest)               |
| `use_ssl`            | boolean      | NO       | Использовать SSL (DEFAULT false)         |
| `search_filter`      | varchar(500) | NO       | Фильтр поиска (DEFAULT `(uid={username})`)|

### `identity_source_links`

| Колонка              | Тип          | Nullable | Описание                          |
| -------------------- | ------------ | -------- | --------------------------------- |
| `id`                 | uuid         | NO       | PK                                |
| `user_id`            | uuid         | NO       | FK → users(id) CASCADE            |
| `identity_source_id` | uuid         | NO       | FK → identity_sources(id) CASCADE |
| `external_identity`  | varchar(500) | NO       | Sub из внешнего токена / username  |
| `created_at`         | timestamptz  | NO       |                                   |

Индекс: `(identity_source_id, external_identity)` UNIQUE.

## API

### CRUD — `/api/identity-sources`

Требует полномочия `system.identity-sources.*` в workspace `system`.

| Метод  | Путь                           | Описание               |
| ------ | ------------------------------ | ---------------------- |
| GET    | `/api/identity-sources`        | Список источников      |
| GET    | `/api/identity-sources/{id}`   | Детали источника       |
| POST   | `/api/identity-sources`        | Создание               |
| PUT    | `/api/identity-sources/{id}`   | Обновление             |
| DELETE | `/api/identity-sources/{id}`   | Удаление (soft delete) |

### Управление связями пользователей — `/api/users/{id}/identity-sources`

Связки пользователей с внешними источниками управляются через Users API.

| Метод | Путь                                  | Описание                              |
| ----- | ------------------------------------- | ------------------------------------- |
| GET   | `/api/users/{id}/identity-sources`    | Список связей пользователя            |
| PUT   | `/api/users/{id}/identity-sources`    | Установить связи (diff: add/remove)   |

PUT-запрос принимает полный список связей. Сервис вычисляет diff и добавляет/удаляет нужные записи.

```json
{
  "links": [
    { "identitySourceId": "...", "externalIdentity": "user@ldap" }
  ]
}
```

## Потоки аутентификации

Федеративная аутентификация реализована двумя отдельными grant types на `/connect/token`.

### JWT Bearer Grant (OIDC)

Аутентификация через внешний OIDC-токен. Использует стандартный grant type `urn:ietf:params:oauth:grant-type:jwt-bearer`.

**Запрос** (form-encoded):

```
grant_type=urn:ietf:params:oauth:grant-type:jwt-bearer
assertion=<внешний_jwt>
scope=openid profile ws:*
client_id=frontend-app
```

**Поток:**

```
Client                    Auth Service                   External IdP
  |                            |                              |
  |  POST /connect/token       |                              |
  |  grant_type=jwt-bearer     |                              |
  |  assertion=<jwt>           |                              |
  |--------------------------->|                              |
  |                            |  Extract issuer from JWT     |
  |                            |  Find OIDC source by issuer  |
  |                            |                              |
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
  |                            |  (amr: ["fed"])              |
  |                            |                              |
  |  { access_token, ... }     |                              |
  |<---------------------------|                              |
```

### LDAP Grant

Аутентификация через LDAP-каталог. Custom grant type `urn:custom:ldap`.

**Запрос** (form-encoded):

```
grant_type=urn:custom:ldap
identity_source=active-directory
username=john.doe
password=secret
scope=openid profile ws:*
client_id=frontend-app
```

**Поток:**

```
Client                    Auth Service                   LDAP Server
  |                            |                              |
  |  POST /connect/token       |                              |
  |  grant_type=urn:custom:ldap|                              |
  |  identity_source=ad        |                              |
  |  username=john.doe         |                              |
  |  password=secret           |                              |
  |--------------------------->|                              |
  |                            |  Find LDAP source by name    |
  |                            |  Load LDAP config            |
  |                            |                              |
  |                            |  LDAP BIND (service account) |
  |                            |----------------------------->|
  |                            |  Search user by filter       |
  |                            |  (uid={username})            |
  |                            |----------------------------->|
  |                            |  Found DN                    |
  |                            |<-----------------------------|
  |                            |  LDAP BIND (user DN + pwd)   |
  |                            |----------------------------->|
  |                            |  Success                     |
  |                            |<-----------------------------|
  |                            |                              |
  |                            |  Find link by                |
  |                            |  (source, username)          |
  |                            |  Check user is active        |
  |                            |  Check 2FA / password change |
  |                            |  Build ClaimsPrincipal       |
  |                            |  (amr: ["pwd"])              |
  |                            |                              |
  |  { access_token, ... }     |                              |
  |<---------------------------|                              |
```

### Промежуточные ответы

Оба grant types могут возвращать:

- `mfa_required` — требуется 2FA, возвращается `mfa_token` и `mfa_channel`
- `password_change_required` — требуется смена пароля, возвращается `challenge_id`

## Валидация внешнего токена (OIDC)

`OidcTokenValidator` использует стандартный OIDC discovery (`/.well-known/openid-configuration`) для получения JWKS.

- Конфигурация кешируется в `ConcurrentDictionary<authority, ConfigurationManager>`
- Issuer извлекается из JWT для автоматического определения OIDC source
- Проверяется подпись, issuer, audience (clientId), срок действия
- Возвращается claim `sub` из токена

## Аутентификация LDAP

`LdapAuthenticator` реализует двухэтапную аутентификацию через Novell.Directory.Ldap:

1. Bind с сервисным аккаунтом (`BindDn` / `BindPassword`)
2. Поиск пользователя по `SearchFilter` (с подстановкой `{username}`)
3. Экранирование LDAP-метасимволов в username
4. Bind с найденным DN и паролем пользователя

## Коды ошибок

| Code                                     | HTTP | Описание                              |
| ---------------------------------------- | ---- | ------------------------------------- |
| `AUTH_IDENTITY_SOURCE_NOT_FOUND`         | 404  | Источник не найден                    |
| `AUTH_IDENTITY_SOURCE_DISABLED`          | 400  | Источник отключен                     |
| `AUTH_IDENTITY_SOURCE_TOKEN_INVALID`     | 401  | Внешний токен невалиден / LDAP auth failed |
| `AUTH_IDENTITY_SOURCE_LINK_NOT_FOUND`    | 401  | Нет связки для данного sub/username   |
| `AUTH_IDENTITY_SOURCE_USER_INACTIVE`     | 401  | Привязанный пользователь неактивен    |
| `AUTH_IDENTITY_SOURCE_DUPLICATE_LINK`    | 400  | Дубликат связки                       |
| `AUTH_IDENTITY_SOURCE_TYPE_MISMATCH`     | 400  | Тип источника не совпадает с запросом |
| `AUTH_IDENTITY_SOURCE_USERNAME_REQUIRED` | 400  | LDAP: username обязателен             |
