# Auth Service

Микросервис аутентификации и авторизации. Управляет пользователями, ролями, разрешениями и рабочими пространствами (workspace multi-tenancy). JWT + refresh tokens, 2FA, RBAC с bitmask-разрешениями.

## Stack
- .NET 8, ASP.NET Core, Entity Framework Core, PostgreSQL
- Clean Architecture: Api → Application → Domain → Infrastructure → Infrastructure.Integration
- OpenSearch (full-text search), Kafka (события через outbox), MailKit (SMTP для 2FA)

## Project Structure
```
src/
  Auth.Api/                         # Controllers, middleware, auth handlers
  Auth.Application/                 # Use cases: Auth, Users, Roles, Permissions, Workspaces, TwoFactor
  Auth.Domain/                      # Entities, value objects
  Auth.Infrastructure/              # EF Core, PostgreSQL, repositories, migrations
  Auth.Infrastructure.Integration/  # OpenSearch, Kafka
tests/
  Auth.UnitTests/        # xUnit, Moq, FluentAssertions, EF InMemory
  Auth.IntegrationTests/ # xUnit, Testcontainers (PostgreSQL), WebApplicationFactory
```

## Key Patterns
- Permission bitmask RBAC: права хранятся как битовые флаги
- Workspace multi-tenancy: пользователь имеет роли в каждом workspace отдельно
- Outbox pattern: события записываются в OutboxMessage → Kafka
- TOTP/OTP 2FA с доставкой по email

## Commands
```bash
dotnet build Auth.sln
python3 run-tests.py --target unit
python3 run-tests.py --target integration
python3 run-tests.py --target all
python3 run-docker.py --env-file .env --profile local-opensearch
```

## Docs
- [docs/architecture.md](docs/architecture.md) — слои, паттерны, структура зависимостей
- [docs/api-conventions.md](docs/api-conventions.md) — стандарт нейминга роутов, форматы запросов/ответов, обработка ошибок
- [docs/database.md](docs/database.md) — схема таблиц, связи, миграции
- [docs/deployment.md](docs/deployment.md) — зависимости, Docker, переменные окружения
- [docs/code.requirements.md](docs/code.requirements.md) — стандарты написания кода в проекте
- [docs/tests.requirements.md](docs/tests.requirements.md) — требования к тестам, что и как покрывать
- [docs/permissions.md](docs/permissions.md) — каталог системных полномочий, bitmask-упаковка
