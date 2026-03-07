# Системные полномочия (Permission Bitmask)

Права хранятся как битовый массив (`byte[]`). Каждое полномочие соответствует определённому биту. Бит `N` находится в байте `N/8`, позиция `N%8`.

Системные полномочия определены в `Auth.Domain.SystemPermissionCatalog` и засеиваются в БД при старте.

## Каталог полномочий

| Бит | Код                                    | Описание                      |
| --- | -------------------------------------- | ----------------------------- |
| 0   | `system.users.view`                    | View users                    |
| 1   | `system.users.create`                  | Create users                  |
| 2   | `system.users.update`                  | Update users                  |
| 3   | `system.users.delete`                  | Delete users                  |
| 4   | `system.roles.view`                    | View roles                    |
| 5   | `system.roles.create`                  | Create roles                  |
| 6   | `system.roles.update`                  | Update roles                  |
| 7   | `system.roles.delete`                  | Delete roles                  |
| 8   | `system.permissions.view`              | View permissions              |
| 9   | `system.permissions.create`            | Create permissions            |
| 10  | `system.permissions.update`            | Update permissions            |
| 11  | `system.permissions.delete`            | Delete permissions            |
| 12  | `system.workspaces.view`               | View workspaces               |
| 13  | `system.workspaces.create`             | Create workspaces             |
| 14  | `system.workspaces.update`             | Update workspaces             |
| 15  | `system.workspaces.delete`             | Delete workspaces             |
| 16  | `system.search.reindex`                | Reindex search                |
| 17  | `system.users.reset-password`          | Reset user password           |
| 18  | `system.notification-templates.view`   | View notification templates   |
| 19  | `system.notification-templates.update` | Update notification templates |
| 20  | `system.identity-sources.view`         | View identity sources         |
| 21  | `system.identity-sources.create`       | Create identity sources       |
| 22  | `system.identity-sources.update`       | Update identity sources       |
| 23  | `system.identity-sources.delete`       | Delete identity sources       |
| 24  | `system.api-clients.view`              | View API clients              |
| 25  | `system.api-clients.create`            | Create API clients            |
| 26  | `system.api-clients.update`            | Update API clients            |
| 27  | `system.api-clients.delete`            | Delete API clients            |
| 28  | `system.permissions.import`            | Import permissions            |
| 29  | `system.permissions.export`            | Export permissions            |
| 30  | `system.roles.import`                  | Import roles                  |
| 31  | `system.roles.export`                  | Export roles                  |
| 32  | `system.workspaces.import`             | Import workspaces             |
| 33  | `system.workspaces.export`             | Export workspaces             |
| 34  | `system.users.import`                  | Import users                  |
| 35  | `system.users.export`                  | Export users                  |

## Упаковка в байты

Биты 0–7 → байт 0, биты 8–15 → байт 1 и т.д.

Пример: полномочия `system.users.view` (бит 0) + `system.roles.view` (бит 4) + `system.permissions.view` (бит 8):

```
Байт 0: 0b00010001 = 0x11  (биты 0 и 4)
Байт 1: 0b00000001 = 0x01  (бит 8)
bitmask = [0x11, 0x01]
```

## JWT-claim `ws`

Токен содержит claim `ws` — словарь `workspaceCode → base64(bitmask)`. Проверка полномочий выполняется в `PermissionHandler` без обращения к БД.

## Резервирование битов

Биты **0–127** зарезервированы для системных полномочий. Пользовательские (кастомные) полномочия назначаются начиная с бита **128** (`SystemPermissionCatalog.CustomBitStart`).

Это гарантирует, что добавление новых системных полномочий в `SystemPermissionCatalog` не конфликтует с уже созданными пользовательскими.

## Системные ограничения

- Системные полномочия (`is_system = true`) нельзя удалить — `AUTH_SYSTEM_PERMISSION_DELETE_FORBIDDEN`.
- Импорт полномочий с битом < 128 запрещён — `AUTH_SYSTEM_PERMISSION_IMPORT_FORBIDDEN`.
- Импорт системных рабочих окружений запрещён — `AUTH_SYSTEM_WORKSPACE_IMPORT_FORBIDDEN`.
- Импорт ролей с несуществующими кодами полномочий — `AUTH_PERMISSION_CODE_NOT_FOUND`.
- Новые системные полномочия добавляются через `SystemPermissionCatalog` с уникальным номером бита в диапазоне 0–127.
