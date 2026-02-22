# Правила создания роутов

Краткий стандарт для новых endpoint-ов в проекте.

## Нейминг

- Используем имена ресурсов во множественном числе: `/api/users`, `/api/roles`.
- В URL только `kebab-case` или простые lowercase сегменты (без `camelCase`).
- Глаголы в URL не добавляем, кроме специальных операций (`/search`, `/refresh`, `/revoke`).
- Параметры пути: `{id:guid}` для сущностей.

## REST API правила

- `GET /api/{resource}` — список.
- `GET /api/{resource}/{id}` — карточка.
- `POST /api/{resource}` — создание.
- `PUT /api/{resource}/{id}` — полное обновление.
- `PATCH /api/{resource}/{id}` — частичное обновление.
- `DELETE /api/{resource}/{id}` — soft/hard delete по контракту сервиса.
- Batch-операции выделяем как подресурсы:
  - `PUT /api/users/{id}/workspaces`
  - `PUT /api/roles/{id}/permissions`
  - `PUT /api/user-workspaces/{id}/roles`

## Правила входных параметров

- DTO для body строго типизированы (никаких `object`, `dynamic`).
- Query/path/body валидируются на уровне контроллера и/или application слоя.
- Для `search` используем единый контракт:

```json
{
  "filter": {
    "<fieldName>": {
      "eq": "<value>",
      "in": ["<value1>", "<value2>"],
      "ts": "<text>"
    }
  },
  "sortBy": "<fieldName>",
  "sortOrder": "ASC",
  "page": 1,
  "pageSize": 20
}
```

- `sortOrder` только `ASC` или `DESC`.
- Для batch-роутов вход должен быть коллекцией идентификаторов/DTO, например:
  - `{ "ids": ["..."] }`
  - `{ "workspaces": [ ... ] }`
  - `{ "permissions": [ ... ] }`

## Правила ответов

- `create` возвращает созданную модель (`200/201` по контракту endpoint-а).
- `update` возвращает измененную модель.
- `patch` возвращает измененную модель.
- `delete` и некоторые batch-операции могут возвращать `204 No Content`.
- Ошибки возвращаются в формате `ProblemDetails`.
