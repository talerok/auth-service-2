# Database

Основные таблицы:

- `users`
- `workspaces`
- `roles`
- `permissions`
- `user_workspaces`
- `user_workspace_roles`
- `role_permissions`
- `refresh_tokens`
- `password_change_challenges`
- `two_factor_challenges`
- `outbox_messages`

Принципы:

- Soft delete через `deleted_at`
- Глобальные query filters (`deleted_at IS NULL`)
- Уникальность для soft delete через partial indexes


### Таблица `users`

| Колонка               | Тип          | Nullable | Описание                                        |
|-----------------------|--------------|----------|-------------------------------------------------|
| `id`                  | uuid         | NO       | PK                                              |
| `username`            | varchar(100) | NO       | Уникальное имя пользователя (partial index)     |
| `email`               | varchar(255) | NO       | Email пользователя (partial index)              |
| `password_hash`       | varchar(200) | NO       | Хэш пароля                                      |
| `is_active`           | boolean      | NO       | Признак активности аккаунта                     |
| `must_change_password`| boolean      | NO       | Флаг обязательной смены пароля (DEFAULT false)  |
| `two_factor_enabled`  | boolean      | NO       | Включена ли двухфакторная аутентификация        |
| `two_factor_channel`  | varchar(16)  | YES      | Канал 2FA (например, Email, SMS)                |
| `created_at`          | timestamptz  | NO       |                                                 |
| `updated_at`          | timestamptz  | NO       |                                                 |
| `deleted_at`          | timestamptz  | YES      | Soft delete                                     |

Индексы:
- `IX_users_Username` UNIQUE WHERE `"DeletedAt" IS NULL`
- `IX_users_Email` UNIQUE WHERE `"DeletedAt" IS NULL`


### Таблица `workspaces`

| Колонка       | Тип          | Nullable | Описание                                    |
|---------------|--------------|----------|---------------------------------------------|
| `id`          | uuid         | NO       | PK                                          |
| `name`        | varchar(120) | NO       | Уникальное название воркспейса (partial index) |
| `description` | varchar(500) | NO       | Описание воркспейса                         |
| `is_system`   | boolean      | NO       | Признак системного воркспейса               |
| `created_at`  | timestamptz  | NO       |                                             |
| `updated_at`  | timestamptz  | NO       |                                             |
| `deleted_at`  | timestamptz  | YES      | Soft delete                                 |

Индексы:
- `IX_workspaces_Name` UNIQUE WHERE `"DeletedAt" IS NULL`


### Таблица `roles`

| Колонка       | Тип          | Nullable | Описание                                  |
|---------------|--------------|----------|-------------------------------------------|
| `id`          | uuid         | NO       | PK                                        |
| `name`        | varchar(120) | NO       | Уникальное название роли (partial index)  |
| `description` | varchar(500) | NO       | Описание роли                             |
| `created_at`  | timestamptz  | NO       |                                           |
| `updated_at`  | timestamptz  | NO       |                                           |
| `deleted_at`  | timestamptz  | YES      | Soft delete                               |

Индексы:
- `IX_roles_Name` UNIQUE WHERE `"DeletedAt" IS NULL`


### Таблица `permissions`

| Колонка       | Тип          | Nullable | Описание                                       |
|---------------|--------------|----------|------------------------------------------------|
| `id`          | uuid         | NO       | PK                                             |
| `bit`         | integer      | NO       | Номер бита для bitmask-операций (UNIQUE)       |
| `code`        | varchar(120) | NO       | Код разрешения (partial index)                 |
| `description` | varchar(500) | NO       | Описание разрешения                            |
| `is_system`   | boolean      | NO       | Признак системного разрешения                  |
| `created_at`  | timestamptz  | NO       |                                                |
| `updated_at`  | timestamptz  | NO       |                                                |
| `deleted_at`  | timestamptz  | YES      | Soft delete                                    |

Индексы:
- `IX_permissions_Bit` UNIQUE
- `IX_permissions_Code` UNIQUE WHERE `"DeletedAt" IS NULL`


### Таблица `user_workspaces`

| Колонка        | Тип  | Nullable | Описание                              |
|----------------|------|----------|---------------------------------------|
| `id`           | uuid | NO       | PK                                    |
| `user_id`      | uuid | NO       | FK → users(id) CASCADE                |
| `workspace_id` | uuid | NO       | FK → workspaces(id) CASCADE           |

Индексы:
- `IX_user_workspaces_UserId_WorkspaceId` UNIQUE
- `IX_user_workspaces_WorkspaceId`


### Таблица `user_workspace_roles`

| Колонка             | Тип  | Nullable | Описание                                |
|---------------------|------|----------|-----------------------------------------|
| `id`                | uuid | NO       | PK                                      |
| `user_workspace_id` | uuid | NO       | FK → user_workspaces(id) CASCADE        |
| `role_id`           | uuid | NO       | FK → roles(id) CASCADE                  |

Индексы:
- `IX_user_workspace_roles_UserWorkspaceId_RoleId` UNIQUE
- `IX_user_workspace_roles_RoleId`


### Таблица `role_permissions`

| Колонка         | Тип  | Nullable | Описание                           |
|-----------------|------|----------|------------------------------------|
| `id`            | uuid | NO       | PK                                 |
| `role_id`       | uuid | NO       | FK → roles(id) CASCADE             |
| `permission_id` | uuid | NO       | FK → permissions(id) CASCADE       |

Индексы:
- `IX_role_permissions_RoleId_PermissionId` UNIQUE
- `IX_role_permissions_PermissionId`


### Таблица `refresh_tokens`

| Колонка      | Тип          | Nullable | Описание                        |
|--------------|--------------|----------|---------------------------------|
| `id`         | uuid         | NO       | PK                              |
| `user_id`    | uuid         | NO       | FK → users(id) CASCADE          |
| `token`      | varchar(200) | NO       | Значение refresh-токена (UNIQUE)|
| `expires_at` | timestamptz  | NO       | Срок действия токена            |
| `created_at` | timestamptz  | NO       |                                 |
| `revoked_at` | timestamptz  | YES      | Время отзыва токена             |

Индексы:
- `IX_refresh_tokens_Token` UNIQUE
- `IX_refresh_tokens_UserId`


### Таблица `password_change_challenges`

| Колонка      | Тип         | Nullable | Описание                                 |
|--------------|-------------|----------|------------------------------------------|
| `id`         | uuid        | NO       | PK; одноразовый токен смены пароля       |
| `user_id`    | uuid        | NO       | FK → users(id) CASCADE                   |
| `expires_at` | timestamptz | NO       | TTL challenge (по умолчанию 15 мин)      |
| `is_used`    | boolean     | NO       | Флаг использования (DEFAULT false)       |
| `created_at` | timestamptz | NO       |                                          |
| `used_at`    | timestamptz | YES      | Время использования challenge            |

Индексы: `IX_password_change_challenges_UserId` на `user_id`.


### Таблица `two_factor_challenges`

| Колонка           | Тип          | Nullable | Описание                                              |
|-------------------|--------------|----------|-------------------------------------------------------|
| `id`              | uuid         | NO       | PK                                                    |
| `user_id`         | uuid         | NO       | FK → users(id) CASCADE                                |
| `channel`         | varchar(16)  | NO       | Канал доставки OTP (например, Email, SMS)             |
| `purpose`         | varchar(32)  | NO       | Назначение challenge (например, Login, ChangeEmail)   |
| `otp_hash`        | varchar(200) | NO       | Хэш одноразового кода                                 |
| `otp_salt`        | varchar(120) | NO       | Salt для хэширования OTP                              |
| `otp_encrypted`   | varchar(512) | NO       | Зашифрованный OTP для повторной доставки              |
| `attempts`        | integer      | NO       | Текущее количество попыток                            |
| `max_attempts`    | integer      | NO       | Максимальное количество попыток                       |
| `delivery_status` | varchar(32)  | NO       | Статус доставки OTP                                   |
| `is_used`         | boolean      | NO       | Флаг использования challenge                          |
| `expires_at`      | timestamptz  | NO       | TTL challenge                                         |
| `created_at`      | timestamptz  | NO       |                                                       |
| `completed_at`    | timestamptz  | YES      | Время завершения challenge                            |

Индексы:
- `IX_two_factor_challenges_UserId`
- `IX_two_factor_challenges_UserId_Purpose`


### Таблица `outbox_messages`

| Колонка        | Тип          | Nullable | Описание                              |
|----------------|--------------|----------|---------------------------------------|
| `id`           | uuid         | NO       | PK                                    |
| `topic`        | varchar(200) | NO       | Топик сообщения (Kafka/MQ)            |
| `key`          | varchar(200) | NO       | Ключ сообщения для партиционирования  |
| `payload`      | text         | NO       | JSON-тело события                     |
| `created_at`   | timestamptz  | NO       |                                       |
| `processed_at` | timestamptz  | YES      | Время успешной отправки               |

