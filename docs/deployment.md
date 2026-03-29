# Deployment

## Dependencies

- PostgreSQL 16+
- OpenSearch 2+
- RabbitMQ 4+
- Redis 7+
- Docker + Docker Compose

## Docker services

`docker-compose.yml` определяет следующие сервисы:

| Сервис | Образ | Профиль | Описание |
|---|---|---|---|
| `auth-api` | build from Dockerfile | — | API-сервер |
| `postgres` | postgres:16-alpine | — | PostgreSQL |
| `rabbitmq` | rabbitmq:4-management-alpine | — | Message broker |
| `redis` | redis:7-alpine | — | Distributed cache |
| `opensearch` | opensearchproject/opensearch:2.15.0 | `local-opensearch` | Full-text search |
| `mailhog` | jcalonso/mailhog:latest | `mailhog` | Mock SMTP (dev) |

## Environment setup

Скопируйте шаблон переменных:

```bash
cp .env .env.local
```

Для Docker-only режима `.env` является единым источником runtime-конфигурации.

## Full pipeline script

Скрипт `run-docker.py` (в корне проекта) выполняет:

- `dotnet build`
- `docker compose up --build -d` с выбранным env-файлом и профилем

```bash
python3 run-docker.py --env-file .env --profile local-opensearch
```

С внешним OpenSearch (без локального контейнера):

```bash
python3 run-docker.py --env-file .env --profile none
```

Пропуск сборки:

```bash
python3 run-docker.py --skip-build
```

### Option A: local OpenSearch in docker-compose

В `.env`:

```bash
OPENSEARCH_URL=http://opensearch:9200
```

```bash
COMPOSE_PROFILES=local-opensearch docker compose up --build -d
```

Поднимаются контейнеры:

- `auth-api`
- `postgres`
- `opensearch`

```bash
curl "http://localhost:${AUTH_API_PORT}/health"
```

Остановка:

```bash
COMPOSE_PROFILES=local-opensearch docker compose down
```

### Option B: external OpenSearch

1. Укажите URL внешнего OpenSearch в `.env`:

```bash
OPENSEARCH_URL=http://<external-host>:9200
```

2. При необходимости задайте логин/пароль в `.env`:

```bash
OPENSEARCH_USERNAME=<username>
OPENSEARCH_PASSWORD=<password>
```

3. Запустите сервис:

```bash
docker compose up --build -d
```

В этом режиме запускаются:

- `auth-api`
- `postgres`

`opensearch` из `docker-compose.yml` не стартует, потому что он включен только в профиль `local-opensearch`.

### MailHog (dev email)

```bash
COMPOSE_PROFILES=mailhog docker compose up -d mailhog
```

- SMTP: `localhost:1025`
- Web UI: `http://localhost:8025`

Можно комбинировать профили:

```bash
COMPOSE_PROFILES=local-opensearch,mailhog docker compose up --build -d
```

## Environment variables

### Ports

| Variable | Default | Description |
|---|---|---|
| `AUTH_API_PORT` | 4000 | Порт API |
| `POSTGRES_PORT` | 5432 | Порт PostgreSQL |
| `RABBITMQ_PORT` | 5672 | Порт RabbitMQ |
| `RABBITMQ_MGMT_PORT` | 15672 | Порт RabbitMQ Management UI |
| `REDIS_PORT` | 6379 | Порт Redis |
| `OPENSEARCH_PORT` | 9200 | Порт OpenSearch |
| `OPENSEARCH_METRICS_PORT` | 9600 | Порт метрик OpenSearch |

### PostgreSQL

| Variable | Description |
|---|---|
| `POSTGRES_CONNECTION_STRING` | Connection string для EF Core |

### OpenSearch

| Variable | Default | Description |
|---|---|---|
| `OPENSEARCH_URL` | — | URL OpenSearch (пустой = NullSearchIndexService) |
| `OPENSEARCH_INDEX_PREFIX` | auth | Префикс индексов |
| `OPENSEARCH_ENSURE_INDICES_ON_STARTUP` | true | Создание индексов при старте |
| `OPENSEARCH_REINDEX_ON_STARTUP` | false | Переиндексация при старте |
| `OPENSEARCH_USERNAME` | — | Логин |
| `OPENSEARCH_PASSWORD` | — | Пароль |

### RabbitMQ

| Variable | Default | Description |
|---|---|---|
| `RABBITMQ_USER` | guest | Логин RabbitMQ |
| `RABBITMQ_PASSWORD` | guest | Пароль RabbitMQ |

### Redis

| Variable | Default | Description |
|---|---|---|
| `Integration__Redis__ConnectionString` | localhost:6379 | Connection string Redis |

### Security

| Variable | Description |
|---|---|
| `ENCRYPTION_KEY` | Ключ шифрования секретов at-rest (min 32 chars, **обязателен**) |

### SMTP

| Variable | Default | Description |
|---|---|---|
| `SMTP_ENABLED` | false | Включить SMTP |
| `SMTP_HOST` | — | SMTP-сервер |
| `SMTP_PORT` | 587 | Порт |
| `SMTP_USE_SSL` | true | SSL |
| `SMTP_USERNAME` | — | Логин |
| `SMTP_PASSWORD` | — | Пароль |
| `SMTP_FROM_EMAIL` | noreply@auth-service | Адрес отправителя |
| `SMTP_FROM_NAME` | Auth Service | Имя отправителя |

### SMS Gateway

| Variable | Default | Description |
|---|---|---|
| `SMS_GATEWAY_ENABLED` | false | Включить SMS |
| `SMS_GATEWAY_BASE_URL` | — | URL промежуточного SMS-сервиса |
| `SMS_GATEWAY_API_KEY` | — | API-ключ |
| `SMS_GATEWAY_TIMEOUT_SECONDS` | 5 | Таймаут HTTP-запроса |

### CORS

| Variable | Description |
|---|---|
| `CORS_ALLOWED_ORIGINS` | Список origins через запятую |

### Verification URLs

| Variable | Description |
|---|---|
| `VERIFICATION_EMAIL_BASE_URL` | Базовый URL для ссылок верификации email |
| `VERIFICATION_PHONE_BASE_URL` | Базовый URL для ссылок верификации телефона |

### OIDC (advanced)

Настраиваются через `Integration__Oidc__*` напрямую (не маппятся в docker-compose env):

| Variable | Default | Description |
|---|---|---|
| `Integration__Oidc__AccessTokenLifetimeMinutes` | 15 | TTL access token |
| `Integration__Oidc__RefreshTokenLifetimeDays` | 7 | TTL refresh token |
| `Integration__Oidc__RefreshTokenReuseLeewaySeconds` | 30 | Окно повторного использования redeemed refresh token |
| `Integration__Oidc__SigningKeyPath` | — | X.509 сертификат подписи (production) |
| `Integration__Oidc__EncryptionKeyPath` | — | X.509 сертификат шифрования (production) |

### Password Expiration (advanced)

| Variable | Default | Description |
|---|---|---|
| `Integration__PasswordExpiration__DefaultMaxAgeDays` | 0 | Глобальный срок действия пароля (0 = отключено) |
