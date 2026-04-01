# Конвенции API

Стандарт для написания новых endpoint-ов в проекте.

## Нейминг роутов

- Имена ресурсов во множественном числе: `/api/users`, `/api/roles`.
- В URL только `kebab-case` или простые lowercase сегменты (без `camelCase`).
- Глаголы в URL не добавляем, кроме специальных операций (`/search`, `/refresh`, `/revoke`).
- Параметры пути: `{id:guid}` для сущностей.

## REST методы

- `GET /api/{resource}` — список.
- `GET /api/{resource}/{id}` — карточка.
- `POST /api/{resource}` — создание.
- `PUT /api/{resource}/{id}` — полное обновление.
- `PATCH /api/{resource}/{id}` — частичное обновление.
- `DELETE /api/{resource}/{id}` — soft/hard delete по контракту сервиса.

Batch-операции выделяем как подресурсы: `PUT /api/roles/{id}/permissions`.

Для чтения связанных коллекций используем GET на подресурс:

- `GET /api/{resource}/{id}/{resource}` — список.

## Входные параметры

- DTO для body строго типизированы (никаких `object`, `dynamic`).
- Query/path/body валидируются на уровне контроллера и/или application слоя.
- Для batch-роутов вход — коллекция идентификаторов/DTO:
  - `{ "ids": ["..."] }` — для набора ID
  - `{ "permissions": [ ... ] }` — для коллекции DTO

## Ответы

- `create` возвращает созданную модель (`200/201` по контракту endpoint-а).
- `update` и `patch` возвращают изменённую модель.
- `delete` и некоторые batch-операции возвращают `204 No Content`.
- Ошибки возвращаются в формате `ProblemDetails` (см. ниже).

## Search

Для поиска используем единый контракт `POST /api/{resource}/search`:

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

Правила:

- `filter` — словарь по имени поля; если не передан, используется `*` (без фильтрации).
- Внутри поля поддерживаются операции `eq`, `in`, `ts`.
- `sortOrder` принимает только `ASC` или `DESC`.

Ответ:

```json
{
  "items": [ ... ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1
}
```

## Ошибки (ProblemDetails)

Ошибки бизнес-логики возвращаются в RFC 7807 формате `application/problem+json` через глобальный middleware.

Правила:

- В `AuthException` передаётся только стабильный `code`.
- Статус (`status`) и заголовок (`title`) определяются централизованно в `AuthProblemDetailsMapper`.
- Человекочитаемое описание (`detail`) также маппится по `code` в том же месте.
- Ошибки authentication/authorization (`401`/`403`) из JWT middleware возвращаются в RFC 7807 через тот же mapper.
- Клиенты должны ориентироваться на `code`, а не на текст `detail`.

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Business rule violation",
  "status": 400,
  "detail": "System permissions cannot be deleted",
  "instance": "/api/permissions/xxx",
  "code": "AUTH_SYSTEM_PERMISSION_DELETE_FORBIDDEN",
  "traceId": "00-..."
}
```

### Коды ошибок

| code                                      | HTTP status |
| ----------------------------------------- | ----------- |
| `AUTH_INVALID_PASSWORD_CHANGE_CHALLENGE`  | 401         |
| `AUTH_INTERNAL_AUTH_DISABLED`             | 403         |
| `AUTH_SYSTEM_PERMISSION_DELETE_FORBIDDEN` | 400         |
| `AUTH_SESSION_NOT_FOUND`                 | 404         |
| `AUTH_SESSION_ALREADY_REVOKED`           | 409         |
| `AUTH_SESSION_REVOKED`                   | 403         |
