# Системные полномочия (Permission Bitmask)

Каждое полномочие принадлежит домену (`Domain`) и имеет номер бита (`Bit`), уникальный внутри домена. Права хранятся как `byte[]` — отдельный bitmask на каждый домен. Бит `N` находится в байте `N/8`, позиция `N%8`.

Системные полномочия определены в `Auth.Domain.SystemPermissionCatalog` и засеиваются в БД при старте.

## Каталог полномочий

### system.users

| Бит | Код              | Описание            |
| --- | ---------------- | ------------------- |
| 0   | `view`           | View users          |
| 1   | `create`         | Create users        |
| 2   | `update`         | Update users        |
| 3   | `delete`         | Delete users        |
| 4   | `reset-password` | Reset user password |
| 5   | `import`         | Import users        |
| 6   | `export`         | Export users        |

### system.roles

| Бит | Код      | Описание     |
| --- | -------- | ------------ |
| 0   | `view`   | View roles   |
| 1   | `create` | Create roles |
| 2   | `update` | Update roles |
| 3   | `delete` | Delete roles |
| 4   | `import` | Import roles |
| 5   | `export` | Export roles |

### system.permissions

| Бит | Код      | Описание           |
| --- | -------- | ------------------ |
| 0   | `view`   | View permissions   |
| 1   | `create` | Create permissions |
| 2   | `update` | Update permissions |
| 3   | `delete` | Delete permissions |
| 4   | `import` | Import permissions |
| 5   | `export` | Export permissions |

### system.workspaces

| Бит | Код      | Описание          |
| --- | -------- | ----------------- |
| 0   | `view`   | View workspaces   |
| 1   | `create` | Create workspaces |
| 2   | `update` | Update workspaces |
| 3   | `delete` | Delete workspaces |
| 4   | `import` | Import workspaces |
| 5   | `export` | Export workspaces |

### system.search

| Бит | Код       | Описание       |
| --- | --------- | -------------- |
| 0   | `reindex` | Reindex search |

### system.notification-templates

| Бит | Код      | Описание                      |
| --- | -------- | ----------------------------- |
| 0   | `view`   | View notification templates   |
| 1   | `update` | Update notification templates |

### system.identity-sources

| Бит | Код      | Описание               |
| --- | -------- | ---------------------- |
| 0   | `view`   | View identity sources  |
| 1   | `create` | Create identity source |
| 2   | `update` | Update identity source |
| 3   | `delete` | Delete identity source |

### system.api-clients

| Бит | Код      | Описание          |
| --- | -------- | ----------------- |
| 0   | `view`   | View API clients  |
| 1   | `create` | Create API client |
| 2   | `update` | Update API client |
| 3   | `delete` | Delete API client |

## Упаковка в байты

Биты 0–7 → байт 0, биты 8–15 → байт 1 и т.д. Каждый домен имеет свой независимый bitmask.

Пример: в домене `system.users` полномочия `view` (бит 0) + `update` (бит 2) + `reset-password` (бит 4):

```
Байт 0: 0b00010101 = 0x15  (биты 0, 2 и 4)
bitmask = [0x15]
```

## JWT-claim `ws`

Токен содержит claim `ws` — вложенный словарь `workspaceCode → domain → base64(bitmask)`.

```json
{
  "ws": {
    "system": {
      "system.users": "fw==",
      "system.roles": "Dw==",
      "system.permissions": "Dw=="
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
- Новые системные полномочия добавляются через `SystemPermissionCatalog` с уникальным доменом и номером бита.
- Кастомные полномочия создаются в любом домене; бит назначается автоматически как `max(bit) + 1` внутри домена.
