# Database

Основные таблицы:

- `users`
- `workspaces`
- `roles`
- `permissions`
- `user_workspaces`
- `user_workspace_roles`
- `role_permissions`
- `applications`
- `service_accounts`
- `service_account_workspaces`
- `service_account_workspace_roles`
- `password_change_challenges`
- `two_factor_challenges`
- `identity_sources`
- `identity_source_oidc_configs`
- `identity_source_ldap_configs`
- `identity_source_links`
- `notification_templates`
- `audit_log_entries`

Принципы:

- Soft delete через `deleted_at`
- Глобальные query filters (`deleted_at IS NULL`)
- Уникальность для soft delete через partial indexes

### Таблица `users`

| Колонка                    | Тип          | Nullable | Описание                                             |
| -------------------------- | ------------ | -------- | ---------------------------------------------------- |
| `id`                       | uuid         | NO       | PK                                                   |
| `username`                 | varchar(100) | NO       | Уникальное имя пользователя (partial index)          |
| `full_name`                | varchar(200) | NO       | Полное имя пользователя                              |
| `email`                    | varchar(255) | NO       | Email пользователя (partial index)                   |
| `phone`                    | varchar(20)  | YES      | Номер телефона                                       |
| `password_hash`            | varchar(200) | NO       | Хэш пароля                                           |
| `is_active`                | boolean      | NO       | Признак активности аккаунта                          |
| `is_internal_auth_enabled` | boolean      | NO       | Разрешена ли аутентификация по паролю (DEFAULT true)  |
| `must_change_password`     | boolean      | NO       | Флаг обязательной смены пароля (DEFAULT false)        |
| `two_factor_enabled`       | boolean      | NO       | Включена ли двухфакторная аутентификация              |
| `two_factor_channel`       | varchar(16)  | YES      | Канал 2FA (например, Email, SMS)                     |
| `locale`                   | varchar(16)  | NO       | Язык пользователя BCP 47 (DEFAULT 'en-US')           |
| `email_verified`           | boolean      | NO       | Подтверждён ли email (DEFAULT false)                 |
| `phone_verified`           | boolean      | NO       | Подтверждён ли телефон (DEFAULT false)               |
| `created_at`               | timestamptz  | NO       |                                                      |
| `updated_at`               | timestamptz  | NO       |                                                      |
| `deleted_at`               | timestamptz  | YES      | Soft delete                                          |

Индексы:

- `IX_users_Username` UNIQUE WHERE `"DeletedAt" IS NULL`
- `IX_users_Email` UNIQUE WHERE `"DeletedAt" IS NULL`

### Таблица `workspaces`

| Колонка       | Тип          | Nullable | Описание                                       |
| ------------- | ------------ | -------- | ---------------------------------------------- |
| `id`          | uuid         | NO       | PK                                             |
| `name`        | varchar(120) | NO       | Уникальное название воркспейса (partial index) |
| `code`        | varchar(64)  | NO       | Уникальный код воркспейса (partial index)      |
| `description` | varchar(500) | NO       | Описание воркспейса                            |
| `is_system`   | boolean      | NO       | Признак системного воркспейса                  |
| `created_at`  | timestamptz  | NO       |                                                |
| `updated_at`  | timestamptz  | NO       |                                                |
| `deleted_at`  | timestamptz  | YES      | Soft delete                                    |

Индексы:

- `IX_workspaces_Name` UNIQUE WHERE `"DeletedAt" IS NULL`
- `IX_workspaces_Code` UNIQUE WHERE `"DeletedAt" IS NULL`

### Таблица `roles`

| Колонка       | Тип          | Nullable | Описание                                 |
| ------------- | ------------ | -------- | ---------------------------------------- |
| `id`          | uuid         | NO       | PK                                       |
| `name`        | varchar(120) | NO       | Уникальное название роли (partial index) |
| `code`        | varchar(64)  | NO       | Уникальный код роли (partial index)      |
| `description` | varchar(500) | NO       | Описание роли                            |
| `created_at`  | timestamptz  | NO       |                                          |
| `updated_at`  | timestamptz  | NO       |                                          |
| `deleted_at`  | timestamptz  | YES      | Soft delete                              |

Индексы:

- `IX_roles_Name` UNIQUE WHERE `"DeletedAt" IS NULL`
- `IX_roles_Code` UNIQUE WHERE `"DeletedAt" IS NULL`

### Таблица `permissions`

| Колонка       | Тип          | Nullable | Описание                                        |
| ------------- | ------------ | -------- | ----------------------------------------------- |
| `id`          | uuid         | NO       | PK                                              |
| `domain`      | varchar(120) | NO       | Домен полномочия (например `system.users`)       |
| `bit`         | integer      | NO       | Номер бита внутри домена для bitmask-операций    |
| `code`        | varchar(120) | NO       | Код действия (например `view`, `create`)         |
| `description` | varchar(500) | NO       | Описание разрешения                              |
| `is_system`   | boolean      | NO       | Признак системного разрешения                    |
| `created_at`  | timestamptz  | NO       |                                                  |
| `updated_at`  | timestamptz  | NO       |                                                  |
| `deleted_at`  | timestamptz  | YES      | Soft delete                                      |

Индексы:

- `IX_permissions_Domain_Bit` UNIQUE ON (`domain`, `bit`)
- `IX_permissions_Domain_Code` UNIQUE ON (`domain`, `code`) WHERE `"DeletedAt" IS NULL`

### Таблица `user_workspaces`

| Колонка        | Тип  | Nullable | Описание                    |
| -------------- | ---- | -------- | --------------------------- |
| `id`           | uuid | NO       | PK                          |
| `user_id`      | uuid | NO       | FK → users(id) CASCADE      |
| `workspace_id` | uuid | NO       | FK → workspaces(id) CASCADE |

Индексы:

- `IX_user_workspaces_UserId_WorkspaceId` UNIQUE
- `IX_user_workspaces_WorkspaceId`

### Таблица `user_workspace_roles`

| Колонка             | Тип  | Nullable | Описание                         |
| ------------------- | ---- | -------- | -------------------------------- |
| `id`                | uuid | NO       | PK                               |
| `user_workspace_id` | uuid | NO       | FK → user_workspaces(id) CASCADE |
| `role_id`           | uuid | NO       | FK → roles(id) CASCADE           |

Индексы:

- `IX_user_workspace_roles_UserWorkspaceId_RoleId` UNIQUE
- `IX_user_workspace_roles_RoleId`

### Таблица `role_permissions`

| Колонка         | Тип  | Nullable | Описание                     |
| --------------- | ---- | -------- | ---------------------------- |
| `id`            | uuid | NO       | PK                           |
| `role_id`       | uuid | NO       | FK → roles(id) CASCADE       |
| `permission_id` | uuid | NO       | FK → permissions(id) CASCADE |

Индексы:

- `IX_role_permissions_RoleId_PermissionId` UNIQUE
- `IX_role_permissions_PermissionId`

### Таблица `password_change_challenges`

| Колонка      | Тип         | Nullable | Описание                            |
| ------------ | ----------- | -------- | ----------------------------------- |
| `id`         | uuid        | NO       | PK; одноразовый токен смены пароля  |
| `user_id`    | uuid        | NO       | FK → users(id) CASCADE              |
| `expires_at` | timestamptz | NO       | TTL challenge (по умолчанию 15 мин) |
| `is_used`    | boolean     | NO       | Флаг использования (DEFAULT false)  |
| `created_at` | timestamptz | NO       |                                     |
| `used_at`    | timestamptz | YES      | Время использования challenge       |

Индексы: `IX_password_change_challenges_UserId` на `user_id`.

### Таблица `two_factor_challenges`

| Колонка           | Тип          | Nullable | Описание                                            |
| ----------------- | ------------ | -------- | --------------------------------------------------- |
| `id`              | uuid         | NO       | PK                                                  |
| `user_id`         | uuid         | NO       | FK → users(id) CASCADE                              |
| `channel`         | varchar(16)  | NO       | Канал доставки OTP (например, Email, SMS)           |
| `purpose`         | varchar(32)  | NO       | Назначение challenge (например, Login, ChangeEmail) |
| `otp_hash`        | varchar(200) | NO       | Хэш одноразового кода                               |
| `otp_salt`        | varchar(120) | NO       | Salt для хэширования OTP                            |
| `otp_encrypted`   | varchar(512) | NO       | Зашифрованный OTP для повторной доставки            |
| `attempts`        | integer      | NO       | Текущее количество попыток                          |
| `max_attempts`    | integer      | NO       | Максимальное количество попыток                     |
| `delivery_status` | varchar(32)  | NO       | Статус доставки OTP                                 |
| `is_used`         | boolean      | NO       | Флаг использования challenge                        |
| `expires_at`      | timestamptz  | NO       | TTL challenge                                       |
| `created_at`      | timestamptz  | NO       |                                                     |
| `completed_at`    | timestamptz  | YES      | Время завершения challenge                          |

Индексы:

- `IX_two_factor_challenges_UserId`
- `IX_two_factor_challenges_UserId_Purpose`

### Таблица `applications`

OAuth2-приложения (authorization code flow). Могут быть public или confidential.

| Колонка                         | Тип           | Nullable | Описание                                            |
| ------------------------------- | ------------- | -------- | --------------------------------------------------- |
| `id`                            | uuid          | NO       | PK                                                  |
| `name`                          | varchar(120)  | NO       | Уникальное название приложения (partial index)      |
| `description`                   | varchar(500)  | YES      | Описание приложения                                 |
| `client_id`                     | varchar(200)  | NO       | Уникальный OAuth ClientId (partial index)           |
| `is_active`                     | boolean       | NO       | Признак активности (DEFAULT true)                   |
| `is_confidential`               | boolean       | NO       | Confidential client (DEFAULT true)                  |
| `logo_url`                      | varchar(2000) | YES      | URL логотипа                                        |
| `homepage_url`                  | varchar(2000) | YES      | URL домашней страницы                               |
| `redirect_uris`                 | jsonb         | NO       | OAuth redirect URIs (DEFAULT '[]'::jsonb)           |
| `post_logout_redirect_uris`     | jsonb         | NO       | Post-logout redirect URIs (DEFAULT '[]'::jsonb)     |
| `allowed_origins`               | jsonb         | NO       | Допустимые CORS origins (DEFAULT '[]'::jsonb)       |
| `scopes`                        | jsonb         | NO       | Разрешённые OAuth scopes (DEFAULT '[]'::jsonb)      |
| `grant_types`                   | jsonb         | NO       | Разрешённые grant types (DEFAULT authorization_code + refresh_token) |
| `audiences`                     | jsonb         | NO       | Список ресурсов (aud claim) (DEFAULT '[]'::jsonb)   |
| `access_token_lifetime_minutes` | integer       | YES      | TTL access-токена в минутах                         |
| `refresh_token_lifetime_minutes`| integer       | YES      | TTL refresh-токена в минутах                        |
| `created_at`                    | timestamptz   | NO       |                                                     |
| `updated_at`                    | timestamptz   | NO       |                                                     |
| `deleted_at`                    | timestamptz   | YES      | Soft delete                                         |

Индексы:

- `IX_applications_Name` UNIQUE WHERE `"DeletedAt" IS NULL`
- `IX_applications_ClientId` UNIQUE WHERE `"DeletedAt" IS NULL`

### Таблица `service_accounts`

Сервисные аккаунты (client credentials flow). Всегда confidential.

| Колонка       | Тип          | Nullable | Описание                                                |
| ------------- | ------------ | -------- | ------------------------------------------------------- |
| `id`                           | uuid         | NO       | PK                                                      |
| `name`                         | varchar(120) | NO       | Уникальное название сервисного аккаунта (partial index) |
| `description`                  | varchar(500) | YES      | Описание                                                |
| `client_id`                    | varchar(200) | NO       | Уникальный OAuth ClientId (partial index)               |
| `is_active`                    | boolean      | NO       | Признак активности (DEFAULT true)                       |
| `audiences`                    | jsonb        | NO       | Список ресурсов (aud claim) (DEFAULT '[]'::jsonb)       |
| `access_token_lifetime_minutes`| integer      | YES      | TTL access-токена в минутах (null = значение по умолчанию)|
| `created_at`                   | timestamptz  | NO       |                                                         |
| `updated_at`                   | timestamptz  | NO       |                                                         |
| `deleted_at`                   | timestamptz  | YES      | Soft delete                                             |

Индексы:

- `IX_service_accounts_Name` UNIQUE WHERE `"DeletedAt" IS NULL`
- `IX_service_accounts_ClientId` UNIQUE WHERE `"DeletedAt" IS NULL`

### Таблица `service_account_workspaces`

| Колонка              | Тип  | Nullable | Описание                          |
| -------------------- | ---- | -------- | --------------------------------- |
| `id`                 | uuid | NO       | PK                                |
| `service_account_id` | uuid | NO       | FK → service_accounts(id) CASCADE |
| `workspace_id`       | uuid | NO       | FK → workspaces(id) CASCADE       |

Индексы:

- `IX_service_account_workspaces_ServiceAccountId_WorkspaceId` UNIQUE

### Таблица `service_account_workspace_roles`

| Колонка                        | Тип  | Nullable | Описание                                   |
| ------------------------------ | ---- | -------- | ------------------------------------------ |
| `id`                           | uuid | NO       | PK                                         |
| `service_account_workspace_id` | uuid | NO       | FK → service_account_workspaces(id) CASCADE |
| `role_id`                      | uuid | NO       | FK → roles(id) CASCADE                     |

Индексы:

- `IX_service_account_workspace_roles_ServiceAccountWorkspaceId_RoleId` UNIQUE

### Таблица `identity_sources`

| Колонка        | Тип          | Nullable | Описание                                 |
| -------------- | ------------ | -------- | ---------------------------------------- |
| `id`           | uuid         | NO       | PK                                       |
| `name`         | varchar(120) | NO       | Уникальное имя источника (partial index) |
| `code`         | varchar(64)  | NO       | Уникальный код источника (partial index) |
| `display_name` | varchar(200) | NO       | Отображаемое имя                         |
| `type`         | varchar(16)  | NO       | Тип источника (oidc, ldap)               |
| `is_enabled`   | boolean      | NO       | Активен ли источник                      |
| `created_at`   | timestamptz  | NO       |                                          |
| `updated_at`   | timestamptz  | NO       |                                          |
| `deleted_at`   | timestamptz  | YES      | Soft delete                              |

Индексы:

- `IX_identity_sources_Name` UNIQUE WHERE `"DeletedAt" IS NULL`
- `IX_identity_sources_Code` UNIQUE WHERE `"DeletedAt" IS NULL`

### Таблица `identity_source_oidc_configs`

| Колонка              | Тип          | Nullable | Описание                          |
| -------------------- | ------------ | -------- | --------------------------------- |
| `id`                 | uuid         | NO       | PK                                |
| `identity_source_id` | uuid         | NO       | FK → identity_sources(id) CASCADE |
| `authority`          | varchar(500) | NO       | OIDC Authority URL                |
| `client_id`          | varchar(200) | NO       | OAuth2 Client ID                  |
| `client_secret`      | varchar(500) | YES      | OAuth2 Client Secret              |

Индексы:

- `IX_identity_source_oidc_configs_IdentitySourceId` UNIQUE

### Таблица `identity_source_ldap_configs`

| Колонка              | Тип          | Nullable | Описание                                 |
| -------------------- | ------------ | -------- | ---------------------------------------- |
| `id`                 | uuid         | NO       | PK                                       |
| `identity_source_id` | uuid         | NO       | FK → identity_sources(id) CASCADE        |
| `host`               | varchar(500) | NO       | LDAP-сервер                              |
| `port`               | integer      | NO       | Порт (389 / 636)                         |
| `base_dn`            | varchar(500) | NO       | Base DN для поиска                       |
| `bind_dn`            | varchar(500) | NO       | DN сервисного аккаунта                   |
| `bind_password`      | varchar(500) | YES      | Пароль сервисного аккаунта               |
| `use_ssl`            | boolean      | NO       | Использовать SSL                         |
| `search_filter`      | varchar(500) | NO       | Фильтр поиска (напр. `(uid={username})`) |

Индексы:

- `IX_identity_source_ldap_configs_IdentitySourceId` UNIQUE

### Таблица `identity_source_links`

| Колонка              | Тип          | Nullable | Описание                               |
| -------------------- | ------------ | -------- | -------------------------------------- |
| `id`                 | uuid         | NO       | PK                                     |
| `user_id`            | uuid         | NO       | FK → users(id) CASCADE                 |
| `identity_source_id` | uuid         | NO       | FK → identity_sources(id) CASCADE      |
| `external_identity`  | varchar(500) | NO       | Внешний идентификатор (sub / username) |
| `created_at`         | timestamptz  | NO       |                                        |

Индексы:

- `IX_identity_source_links_IdentitySourceId_ExternalIdentity` UNIQUE
- `IX_identity_source_links_UserId`

### Таблица `notification_templates`

| Колонка      | Тип          | Nullable | Описание                                              |
| ------------ | ------------ | -------- | ----------------------------------------------------- |
| `id`         | uuid         | NO       | PK                                                    |
| `type`       | varchar(32)  | NO       | Тип шаблона (twofactoremail, twofactorsms, emailverification, phoneverification) |
| `locale`     | varchar(16)  | NO       | Язык шаблона BCP 47 (DEFAULT 'en-US')                 |
| `subject`    | varchar(500) | NO       | Тема письма (пустая строка для SMS)                   |
| `body`       | text         | NO       | Тело шаблона (HTML для email, текст для SMS)          |
| `created_at` | timestamptz  | NO       |                                                       |
| `updated_at` | timestamptz  | NO       |                                                       |
| `deleted_at` | timestamptz  | YES      | Soft delete                                           |

Индексы:

- `IX_notification_templates_Type_Locale` UNIQUE ON (`Type`, `Locale`) WHERE `"DeletedAt" IS NULL`

### Таблица `audit_log_entries`

| Колонка          | Тип          | Nullable | Описание                                       |
| ---------------- | ------------ | -------- | ---------------------------------------------- |
| `id`             | uuid         | NO       | PK                                             |
| `timestamp`      | timestamptz  | NO       | Время события                                  |
| `actor_id`       | uuid         | YES      | ID субъекта (пользователь или сервисный аккаунт) |
| `actor_name`     | varchar(200) | YES      | Имя субъекта                                   |
| `actor_type`     | varchar(50)  | NO       | Тип субъекта (user, serviceaccount, system)    |
| `entity_type`    | varchar(100) | NO       | Тип сущности (user, role, permission, ...)     |
| `entity_id`      | uuid         | NO       | ID затронутой сущности                         |
| `action`         | varchar(100) | NO       | Действие (create, update, delete, ...)         |
| `details`        | jsonb        | YES      | Детали изменений (diff полей)                  |
| `ip_address`     | varchar(45)  | YES      | IP-адрес клиента                               |
| `user_agent`     | varchar(500) | YES      | User-Agent клиента                             |
| `correlation_id` | varchar(64)  | YES      | Корреляционный ID запроса                      |

Индексы:

- `IX_audit_log_entries_EntityType_EntityId` ON (`EntityType`, `EntityId`)
- `IX_audit_log_entries_ActorId` ON (`ActorId`)
- `IX_audit_log_entries_Timestamp` DESC ON (`Timestamp`)
