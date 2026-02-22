# Architecture

Сервис построен по слоям:

- `Auth.Domain` - сущности, системные permission-коды
- `Auth.Application` - контракты use-case сервисов и DTO
- `Auth.Infrastructure` - EF Core, реализация сервисов, seed
- `Auth.Infrastructure.Integration` - OpenSearch интеграция
- `Auth.Api` - контроллеры, middleware, authN/authZ

JWT содержит claim `ws` в формате `workspaceId -> base64(bitmask)`.

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
