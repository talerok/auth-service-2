# Auth Service

Микросервис аутентификации и авторизации на ASP.NET Core 8. OIDC-совместимые эндпоинты, RBAC с bitmask-разрешениями, workspace multi-tenancy, 2FA (TOTP/email/SMS), управление сессиями, аккаунтами, сервисными аккаунтами, приложениями, аудит-логами и полнотекстовым поиском.

## Быстрый старт (Docker)

Требования:

- Docker Desktop / Docker Engine + `docker compose`

Подготовка окружения:

```bash
cp .env.example .env
```

Конфигурация берётся из env-файла (`.env`).

### Скрипт билда и запуска

```bash
python3 .scripts/run-docker.py --env-file .env --profile local-opensearch
```

Полезные опции:

```bash
python3 .scripts/run-docker.py --env-file .env --profile none   # без OpenSearch
python3 .scripts/run-docker.py --env-file .env --skip-build
```

### Скрипт запуска тестов

```bash
python3 .scripts/run-tests.py --target all
```

Примеры:

```bash
python3 .scripts/run-tests.py --target unit
python3 .scripts/run-tests.py --target integration
python3 .scripts/run-tests.py --target integration --filter "FullyQualifiedName~AuthController"
python3 .scripts/run-tests.py --target all --skip-build --no-restore
```

### Остановка

```bash
COMPOSE_PROFILES=local-opensearch docker compose --env-file .env down
```

### Запуск со своим OpenSearch

В `.env`:

```bash
OPENSEARCH_URL=http://<host>:9200
OPENSEARCH_USERNAME=<username>
OPENSEARCH_PASSWORD=<password>
```

```bash
docker compose up --build -d
```

Что поднимается:

- `auth-api` на `http://localhost:${AUTH_API_PORT}`
- `postgres`
- `redis`
- `rabbitmq`

С профилем `local-opensearch` добавляется `opensearch`.
С профилем `mailhog` добавляется локальный SMTP-сервер.

Проверка:

```bash
curl "http://localhost:${AUTH_API_PORT}/health"
```

## Переменные окружения

| Переменная | Описание |
|---|---|
| `AUTH_API_PORT` | Внешний порт API |
| `POSTGRES_PORT` | Внешний порт PostgreSQL |
| `POSTGRES_CONNECTION_STRING` | Строка подключения PostgreSQL |
| `REDIS_CONNECTION_STRING` | Строка подключения Redis (rate limiting) |
| `RABBITMQ_USER`, `RABBITMQ_PASSWORD` | Учётные данные RabbitMQ |
| `OPENSEARCH_URL` | URL OpenSearch |
| `OPENSEARCH_INDEX_PREFIX` | Префикс индексов OpenSearch |
| `OPENSEARCH_ENSURE_INDICES_ON_STARTUP` | Создавать индексы при старте |
| `OPENSEARCH_REINDEX_ON_STARTUP` | Переиндексировать при старте |
| `OPENSEARCH_USERNAME`, `OPENSEARCH_PASSWORD` | Учётные данные OpenSearch |
| `JWT_SECRET` | Секрет JWT (минимум 32 символа) |
| `ENCRYPTION_KEY` | Ключ AES-GCM для шифрования OTP и sensitive-полей (минимум 32 символа) |
| `SMTP_ENABLED` | Включить SMTP для 2FA |
| `SMTP_HOST`, `SMTP_PORT` | SMTP-сервер |
| `SMTP_USE_SSL`, `SMTP_USERNAME`, `SMTP_PASSWORD` | SMTP-параметры |
| `SMTP_FROM_EMAIL`, `SMTP_FROM_NAME` | Отправитель писем |
| `SMS_GATEWAY_ENABLED` | Включить SMS-шлюз для 2FA |
| `SMS_GATEWAY_BASE_URL`, `SMS_GATEWAY_API_KEY` | SMS-шлюз |
| `CORS_ALLOWED_ORIGINS` | Разрешённые origins (через запятую) |
| `VERIFICATION_EMAIL_BASE_URL` | Базовый URL для верификации email |
| `VERIFICATION_PHONE_BASE_URL` | Базовый URL для верификации телефона |
| `INTEGRATION__RATELIMIT__AUTH__*` | Rate limit для auth-эндпоинтов |
| `INTEGRATION__RATELIMIT__TWOFACTOR__*` | Rate limit для 2FA-эндпоинтов |
| `INTEGRATION__RATELIMIT__GLOBAL__*` | Глобальный rate limit |
| `INTEGRATION__CLEANUP__*` | Интервалы и retention для фоновой очистки |

## Основные эндпоинты

### OIDC / Connect
- `POST /connect/login`
- `POST /connect/token`
- `GET/POST /connect/authorize`
- `POST /connect/authorize/consent`
- `POST /connect/mfa/verify`
- `GET/POST /connect/userinfo`
- `GET/POST /connect/logout`
- `GET /connect/client-info`

### Аккаунт
- `GET/PATCH /api/account`
- `POST /api/account/2fa/enable`
- `POST /api/account/2fa/confirm`
- `POST /api/account/verify-email/send`
- `POST /api/account/verify-email/confirm`
- `POST /api/account/verify-phone/send`
- `POST /api/account/verify-phone/confirm`
- `POST /api/account/password/forced-change`
- `GET /api/account/password-requirements`

### Сессии
- `GET /api/sessions`
- `DELETE /api/sessions`
- `DELETE /api/sessions/{id}`
- `POST /api/sessions/search`
- `GET /api/users/{userId}/sessions`
- `DELETE /api/users/{userId}/sessions`
- `DELETE /api/users/{userId}/sessions/{id}`

### Пользователи
- `GET/POST /api/users`
- `GET/PUT/PATCH/DELETE /api/users/{id}`
- `GET /api/users/{id}/workspaces`
- `PUT /api/users/{id}/workspaces`
- `POST /api/users/{id}/reset-password`
- `POST /api/users/{id}/verify-email/send`
- `POST /api/users/{id}/verify-phone/send`
- `POST /api/users/search`
- `POST /api/users/import`
- `GET /api/users/export`

### Роли, разрешения, рабочие пространства
- `GET/POST/PUT/DELETE /api/roles`
- `GET /api/roles/{id}/permissions`
- `PUT /api/roles/{id}/permissions`
- `GET/POST/PUT/DELETE /api/permissions`
- `GET/POST/PUT/DELETE /api/workspaces`
- `GET /api/workspaces/{id}/workspaces`
- `PUT /api/workspaces/{id}/workspaces`

### Сервисные аккаунты и приложения
- `GET/POST/PUT/DELETE /api/service-accounts`
- `POST /api/service-accounts/{id}/regenerate-secret`
- `GET/POST/PUT/DELETE /api/applications`

### Identity sources и шаблоны уведомлений
- `GET/POST/PUT/DELETE /api/identity-sources`
- `GET /api/users/{id}/identity-sources`
- `PUT /api/users/{id}/identity-sources`
- `GET/POST/PUT/DELETE /api/notification-templates`

### Аудит-логи и поиск
- `GET /api/audit-logs`
- `POST /api/audit-logs/search`
- `POST /api/search`

### Переиндексация
- `POST /api/search/reindex`
- `POST /api/search/reindex/users`
- `POST /api/search/reindex/roles`
- `POST /api/search/reindex/permissions`
- `POST /api/search/reindex/workspaces`
- `POST /api/search/reindex/sessions`
- `POST /api/search/reindex/service-accounts`
- `POST /api/search/reindex/applications`
- `POST /api/search/reindex/audit-logs`

## Seed

- Workspace `default`
- Role `admin`
- User `admin/admin`
- Системные permissions bits `0..15`

## Примеры API

Логин:

```bash
curl -X POST "http://localhost:5000/connect/login" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "admin"
  }'
```

Поиск пользователей:

```bash
curl -X POST "http://localhost:5000/api/users/search" \
  -H "Authorization: Bearer <ACCESS_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "filter": {
      "username": { "ts": "admin" },
      "isActive": { "eq": "true" }
    },
    "sortBy": "username",
    "sortOrder": "ASC",
    "page": 1,
    "pageSize": 20
  }'
```

## Лицензия

[MIT](LICENSE)
