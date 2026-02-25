# Системные полномочия (Permission Bitmask)

Права хранятся как битовый массив (`byte[]`). Каждое полномочие соответствует определённому биту. Бит `N` находится в байте `N/8`, позиция `N%8`.

Системные полномочия определены в `Auth.Domain.SystemPermissionCatalog` и засеиваются в БД при старте.

## Каталог полномочий

| Бит | Код                  | Описание           |
| --- | -------------------- | ------------------ |
| 0   | `users.view`         | View users         |
| 1   | `users.create`       | Create users       |
| 2   | `users.update`       | Update users       |
| 3   | `users.delete`       | Delete users       |
| 4   | `roles.view`         | View roles         |
| 5   | `roles.create`       | Create roles       |
| 6   | `roles.update`       | Update roles       |
| 7   | `roles.delete`       | Delete roles       |
| 8   | `permissions.view`   | View permissions   |
| 9   | `permissions.create` | Create permissions |
| 10  | `permissions.update` | Update permissions |
| 11  | `permissions.delete` | Delete permissions |
| 12  | `workspaces.view`    | View workspaces    |
| 13  | `workspaces.create`  | Create workspaces  |
| 14  | `workspaces.update`  | Update workspaces  |
| 15  | `workspaces.delete`  | Delete workspaces  |
| 16  | `search.reindex`     | Reindex search     |

## Упаковка в байты

Биты 0–7 → байт 0, биты 8–15 → байт 1 и т.д.

Пример: полномочия `users.view` (бит 0) + `roles.view` (бит 4) + `permissions.view` (бит 8):

```
Байт 0: 0b00010001 = 0x11  (биты 0 и 4)
Байт 1: 0b00000001 = 0x01  (бит 8)
bitmask = [0x11, 0x01]
```

## JWT-claim `ws`

Токен содержит claim `ws` — словарь `workspaceId → base64(bitmask)`. Проверка полномочий выполняется в `PermissionHandler` без обращения к БД.

## Системные ограничения

- Системные полномочия (`is_system = true`) нельзя удалить — `AUTH_SYSTEM_PERMISSION_DELETE_FORBIDDEN`.
- Новые полномочия добавляются через `SystemPermissionCatalog` с уникальным номером бита.
