# Deployment

## Dependencies

- PostgreSQL 14+
- OpenSearch 2+
- Docker + Docker Compose

## Run in Docker

`docker-compose.yml` поддерживает два режима запуска сервиса и ориентирован на Docker-only runtime.

## Environment setup

Скопируйте шаблон переменных:

```bash
cp .env.example .env
```

Для Docker-only режима `.env` является единым источником runtime-конфигурации.

## Full pipeline script

Скрипт `scripts/run-docker.py` выполняет:
- `dotnet restore`
- `dotnet build`
- `dotnet test`
- `docker compose up --build -d` c выбранным env-файлом

Пример:

```bash
python3 ./scripts/run-docker.py local --env-file .env.example
```

С внешним OpenSearch:

```bash
python3 ./scripts/run-docker.py external --env-file .env
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

## Useful environment variables

- `AUTH_API_PORT`
- `POSTGRES_PORT`
- `OPENSEARCH_PORT`
- `OPENSEARCH_METRICS_PORT`
- `POSTGRES_CONNECTION_STRING`
- `OPENSEARCH_URL`
- `OPENSEARCH_INDEX_PREFIX`
- `OPENSEARCH_ENSURE_INDICES_ON_STARTUP`
- `OPENSEARCH_REINDEX_ON_STARTUP`
- `OPENSEARCH_USERNAME`, `OPENSEARCH_PASSWORD`
- `KAFKA_BOOTSTRAP_SERVERS`
- `JWT_SECRET` (must be at least 32 chars)
- `KAFKA_ENABLED=true` with `COMPOSE_PROFILES=kafka` to enable local Kafka
