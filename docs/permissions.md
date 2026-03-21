# Системные полномочия (Permission Bitmask)

Каждое полномочие принадлежит домену (`Domain`) и имеет номер бита (`Bit`), уникальный внутри домена. Права хранятся как `byte[]` — отдельный bitmask на каждый домен. Бит `N` находится в байте `N/8`, позиция `N%8`.

Все системные полномочия находятся в домене `system`. Код полномочия имеет формат `system.<entity>.<action>`.

Системные полномочия определены в `Auth.Domain.SystemPermissionCatalog` и засеиваются в БД при старте.

## Каталог полномочий (домен `system`)

| Бит | Код                                    | Описание                      |
| --- | -------------------------------------- | ----------------------------- |
| 0   | `system.users.view`                    | View users                    |
| 1   | `system.users.create`                  | Create users                  |
| 2   | `system.users.update`                  | Update users                  |
| 3   | `system.users.delete`                  | Delete users                  |
| 4   | `system.users.reset-password`          | Reset user password           |
| 5   | `system.users.import`                  | Import users                  |
| 6   | `system.users.export`                  | Export users                  |
| 7   | `system.roles.view`                    | View roles                    |
| 8   | `system.roles.create`                  | Create roles                  |
| 9   | `system.roles.update`                  | Update roles                  |
| 10  | `system.roles.delete`                  | Delete roles                  |
| 11  | `system.roles.import`                  | Import roles                  |
| 12  | `system.roles.export`                  | Export roles                  |
| 13  | `system.permissions.view`              | View permissions              |
| 14  | `system.permissions.create`            | Create permissions            |
| 15  | `system.permissions.update`            | Update permissions            |
| 16  | `system.permissions.delete`            | Delete permissions            |
| 17  | `system.permissions.import`            | Import permissions            |
| 18  | `system.permissions.export`            | Export permissions            |
| 19  | `system.workspaces.view`               | View workspaces               |
| 20  | `system.workspaces.create`             | Create workspaces             |
| 21  | `system.workspaces.update`             | Update workspaces             |
| 22  | `system.workspaces.delete`             | Delete workspaces             |
| 23  | `system.workspaces.import`             | Import workspaces             |
| 24  | `system.workspaces.export`             | Export workspaces             |
| 25  | `system.search.reindex`                | Reindex search                |
| 26  | `system.notification-templates.view`   | View notification templates   |
| 27  | `system.notification-templates.update` | Update notification templates |
| 28  | `system.identity-sources.view`         | View identity sources         |
| 29  | `system.identity-sources.create`       | Create identity source        |
| 30  | `system.identity-sources.update`       | Update identity source        |
| 31  | `system.identity-sources.delete`       | Delete identity source        |
| 32  | `system.applications.view`             | View applications             |
| 33  | `system.applications.create`           | Create applications           |
| 34  | `system.applications.update`           | Update applications           |
| 35  | `system.applications.delete`           | Delete applications           |
| 36  | `system.service-accounts.view`         | View service accounts         |
| 37  | `system.service-accounts.create`       | Create service accounts       |
| 38  | `system.service-accounts.update`       | Update service accounts       |
| 39  | `system.service-accounts.delete`       | Delete service accounts       |

## Упаковка в байты

Биты 0–7 → байт 0, биты 8–15 → байт 1 и т.д. Все системные полномочия находятся в одном домене `system`, поэтому используется единый bitmask.

Пример: полномочия `system.users.view` (бит 0) + `system.users.update` (бит 2) + `system.users.reset-password` (бит 4):

```
Байт 0: 0b00010101 = 0x15  (биты 0, 2 и 4)
bitmask = [0x15, 0x00, 0x00, 0x00, 0x00]
```

## JWT-claim `ws`

Токен содержит claim `ws` — вложенный словарь `workspaceCode → domain → base64(bitmask)`.

```json
{
  "ws": {
    "system": {
      "system": "//8f8A=="
    }
  }
}
```

Проверка полномочий выполняется в `PermissionInHandler` без обращения к БД: по workspace находится словарь доменов, по домену — bitmask, в котором проверяется нужный бит.

## Системные ограничения

- Системные полномочия (`is_system = true`) нельзя удалить — `AUTH_SYSTEM_PERMISSION_DELETE_FORBIDDEN`.
- Импорт поверх системных полномочий запрещён — `AUTH_SYSTEM_PERMISSION_IMPORT_FORBIDDEN`.
- Импорт системных рабочих окружений запрещён — `AUTH_SYSTEM_WORKSPACE_IMPORT_FORBIDDEN`.
- Импорт ролей с несуществующими кодами полномочий — `AUTH_PERMISSION_CODE_NOT_FOUND`.
- Новые системные полномочия добавляются через `SystemPermissionCatalog` с уникальным номером бита.
- Кастомные полномочия создаются в любом домене; бит назначается автоматически как `max(bit) + 1` внутри домена.
