# Auth Service

Auth service на ASP.NET Core 8 с Clean Architecture, JWT аутентификацией, refresh tokens, permission bitmask авторизацией и CRUD API.

## Быстрый старт (Docker)

Требования:

- Docker Desktop / Docker Engine + `docker compose`

Подготовка окружения:

```bash
cp .env.example .env
```

Конфигурация runtime берется из env-файла (`.env` или `.env.example`), `appsettings.Integration.json` больше не используется.

### Скрипт билда и запуска

Скрипт выполняет `build`, затем поднимает `docker compose` c выбранным env-файлом:

```bash
python3 .scripts/run-docker.py --env-file .env --profile local-opensearch
```

Полезные опции:

```bash
python3 .scripts/run-docker.py --env-file .env --profile none
python3 .scripts/run-docker.py --env-file .env --skip-build
```

### Скрипт запуска тестов

Отдельный скрипт для запуска тестов:

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

Остановка:

```bash
COMPOSE_PROFILES=local-opensearch docker compose --env-file .env.example down
```

### 1) Запуск со своим OpenSearch (локально в docker-compose)

В `.env` оставьте:

```bash
OPENSEARCH_URL=http://opensearch:9200
```

```bash
COMPOSE_PROFILES=local-opensearch docker compose up --build -d
```

Что поднимается:

- `auth-api` (API на `http://localhost:${AUTH_API_PORT}`)
- `postgres`
- `opensearch`

Проверка:

```bash
curl "http://localhost:${AUTH_API_PORT}/health"
```

Остановка:

```bash
COMPOSE_PROFILES=local-opensearch docker compose down
```

### 2) Запуск с внешним OpenSearch

Перед запуском укажите адрес внешнего OpenSearch в `.env`:

```bash
OPENSEARCH_URL=http://<host>:9200
OPENSEARCH_USERNAME=<username>
OPENSEARCH_PASSWORD=<password>
```

Запуск:

```bash
docker compose up --build -d
```

Что поднимается:

- `auth-api` (API на `http://localhost:${AUTH_API_PORT}`)
- `postgres`
- `opensearch` не запускается

Остановка:

```bash
docker compose down
```

### Полезные переменные окружения

- `AUTH_API_PORT` - внешний порт API
- `POSTGRES_PORT` - внешний порт PostgreSQL
- `POSTGRES_CONNECTION_STRING` - строка подключения PostgreSQL для `auth-api`
- `OPENSEARCH_URL` - URL OpenSearch для `auth-api`
- `OPENSEARCH_INDEX_PREFIX`, `OPENSEARCH_ENSURE_INDICES_ON_STARTUP`, `OPENSEARCH_REINDEX_ON_STARTUP`
- `OPENSEARCH_USERNAME`, `OPENSEARCH_PASSWORD`
- `JWT_SECRET` - секрет JWT (минимум 32 символа)
- `KAFKA_ENABLED=true`, `KAFKA_BOOTSTRAP_SERVERS` и `COMPOSE_PROFILES=kafka` - включить Kafka локально

## Основные endpoint-ы

- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/register`
- `POST /api/auth/revoke`
- `GET/POST/PUT/DELETE /api/users`
- `GET/POST/PUT/DELETE /api/roles`
- `GET/POST/PUT/DELETE /api/permissions`
- `GET/POST/PUT/DELETE /api/workspaces`
- `PUT /api/users/{id}/workspaces`
- `PUT /api/user-workspaces/{id}/roles`
- `PUT /api/roles/{id}/permissions`

## Seed

- Workspace `default`
- Role `admin`
- User `admin/admin`
- Системные permissions bits `0..15`

## Примеры API

Логин:

```bash
curl -X POST "http://localhost:5000/api/auth/login" \
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
      "username": {
        "ts": "admin"
      },
      "isActive": {
        "eq": "true"
      }
    },
    "sortBy": "username",
    "sortOrder": "ASC",
    "page": 1,
    "pageSize": 20
  }'
```
