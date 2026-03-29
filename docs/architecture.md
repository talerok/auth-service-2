# Architecture

Сервис построен по слоям:

- `Auth.Domain` - сущности, системные permission-коды
- `Auth.Application` - контракты use-case сервисов, DTO, event contracts (`IEventBus`, `IIntegrationEvent`)
- `Auth.Infrastructure` - EF Core, MassTransit (RabbitMQ), Redis, реализация сервисов, consumers
- `Auth.Infrastructure.Integration` - OpenSearch интеграция
- `Auth.Api` - контроллеры, middleware, authN/authZ, health checks

JWT содержит per-workspace claims `ws:<code>` с bitmask-полномочиями. Скоупы запрашиваются в формате `ws:<workspaceCode>`.

## Messaging

Асинхронная обработка через MassTransit + RabbitMQ с transactional outbox (EF Core). Domain events для внешних потребителей, internal commands для OpenSearch индексации и OTP delivery.

Redis используется как distributed cache (permission bitmask, CORS origins) для горизонтального масштабирования.

Подробнее: [docs/messaging.md](messaging.md).

## Error handling

- Бизнес-ошибки выбрасываются как `AuthException(code)` без текстовых сообщений.
- HTTP-ответ формируется в `ProblemDetailsMiddleware`.
- Поля `status`, `title` и `detail` берутся из централизованного `AuthProblemDetailsMapper` по коду ошибки.
- В ответе всегда присутствует `code` для стабильной обработки на клиентах.

## Runtime configuration

Проект использует Docker-only runtime-конфигурацию:

- значения секции `Integration` приходят из env-переменных `Integration__...`;
- источник значений для локального запуска в Docker: `.env` (или `.env.example` через `--env-file`);
- режимы OpenSearch переключаются через `COMPOSE_PROFILES=local-opensearch` и `OPENSEARCH_URL`.
