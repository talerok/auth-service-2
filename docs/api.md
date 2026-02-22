# API

Базовый REST-контракт:

- CRUD: `users`, `roles`, `permissions`, `workspaces`
- Auth: `login`, `refresh`, `register`, `revoke`
- Batch:
  - `PUT /api/users/{id}/workspaces`
  - `PUT /api/user-workspaces/{id}/roles`
  - `PUT /api/roles/{id}/permissions`
- Search:
  - `POST /api/users/search`
  - `POST /api/roles/search`
  - `POST /api/permissions/search`
  - `POST /api/workspaces/search`

## Примеры запросов/ответов

### `POST /api/auth/login`

Request:

```json
{
  "username": "admin",
  "password": "admin"
}
```

Response `200 OK` (обычный вход):

```json
{
  "requiresTwoFactor": false,
  "requiresPasswordChange": false,
  "tokens": {
    "accessToken": "<jwt>",
    "refreshToken": "<refresh-token>",
    "expiresAt": "2026-01-01T12:00:00Z"
  },
  "challengeId": null,
  "channel": null,
  "passwordChangeChallengeId": null
}
```

Response `200 OK` (когда `MustChangePassword == true`):

```json
{
  "requiresTwoFactor": false,
  "requiresPasswordChange": true,
  "passwordChangeChallengeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "tokens": null,
  "challengeId": null,
  "channel": null
}
```

### `POST /api/auth/password/forced-change`

AllowAnonymous

Request:

```json
{
  "challengeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "newPassword": "NewSecurePassword1!"
}
```

Response `200 OK`:

```json
{
  "accessToken": "<jwt>",
  "refreshToken": "<refresh-token>",
  "expiresAt": "2026-01-01T12:15:00Z"
}
```

Response `401 Unauthorized`:

```json
{
  "type": "https://httpstatuses.com/401",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Invalid or expired password change token",
  "code": "AUTH_INVALID_PASSWORD_CHANGE_CHALLENGE"
}
```

### `POST /api/auth/refresh`

Request:

```json
{
  "refreshToken": "<refresh-token>"
}
```

Response `200 OK`:

```json
{
  "accessToken": "<new-jwt>",
  "refreshToken": "<new-refresh-token>",
  "expiresAt": "2026-01-01T12:15:00Z"
}
```

### `PUT /api/roles/{id}/permissions`

Request:

```json
{
  "ids": [
    "d4d00bf2-4548-4f25-b6e2-1be56b3fbb9e",
    "b8e1a62d-62b4-4f6a-99f4-2f5ee41f4805"
  ]
}
```

Response `204 No Content`

### `POST /api/users/search`

Request:

```json
{
  "filter": {
    "username": {
      "ts": "admin"
    },
    "isActive": {
      "eq": "true"
    },
    "workspaceId": {
      "in": [
        "f6d91933-379c-47f1-b629-ec5c6fefc0d9",
        "b2670399-2763-4463-8eba-2dcbf6933ba8"
      ]
    }
  },
  "sortBy": "username",
  "sortOrder": "ASC",
  "page": 1,
  "pageSize": 20
}
```

Response `200 OK`:

```json
{
  "items": [
    {
      "id": "fdddb537-68c2-4bbd-b587-3203fddac58c",
      "username": "admin",
      "email": "admin@local",
      "isActive": true
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1
}
```

Контракт `search` для всех сущностей (`users`, `roles`, `permissions`, `workspaces`):

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
- `filter` — словарь по имени поля.
- Внутри поля поддерживаются операции `eq`, `in`, `ts`.
- `sortOrder` принимает только `ASC` или `DESC`.
- Если `filter` не передан, используется `*` (без фильтрации).

### Ошибки (ProblemDetails)

Ошибки бизнес-логики возвращаются в RFC 7807 формате `application/problem+json` через глобальный middleware.

Правила:
- в `AuthException` передается только стабильный `code`;
- статус (`status`) и заголовок (`title`) определяются централизованно в `AuthProblemDetailsMapper`;
- человекочитаемое описание (`detail`) также маппится по `code` в том же месте;
- ошибки authentication/authorization (`401`/`403`) из JWT middleware также возвращаются в RFC 7807 через тот же mapper;
- клиенты должны ориентироваться на `code`, а не на текст `detail`.

Response `400 Bad Request`:

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

Коды ошибок:

| code | HTTP status |
|------|-------------|
| `AUTH_INVALID_PASSWORD_CHANGE_CHALLENGE` | 401 |
