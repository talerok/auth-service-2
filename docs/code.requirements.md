# Code Requirements

Version: 1  
Status: draft  
Last updated: 2026-02-18

---

## 1. General Principles

- SOLID: SRP (один класс — одна причина изменений), OCP (расширение через абстракции), LSP (подтипы полностью заменяемы, без `NotImplementedException`), ISP (маленькие интерфейсы), DIP (зависимость от абстракций)
- DDD: богатая доменная модель с поведением, никаких анемичных моделей; инварианты защищены конструкторами/методами
- TDD: Red-Green-Refactor — тесты пишутся до реализации

## 2. Architecture Rules

- Clean Architecture: `Domain ← Application ← Infrastructure / API`
- **Domain** — ноль внешних зависимостей (ни NuGet, ни ссылок на другие проекты)
- **Application** — зависит только от Domain; use cases как MediatR Commands/Queries (CQRS); каждый use case в своей папке (`{Feature}/{Command|Query}/{Name}/`)
- **Infrastructure** — зависит от Application и Domain; EF Core, внешние сервисы, одна `DependencyInjection.cs`
- **API** — тонкие контроллеры (только MediatR dispatch); зависит от Application, но не напрямую от Infrastructure (кроме DI-регистрации)
- Бизнес-логика запрещена в контроллерах и репозиториях; доменные сущности не возвращаются из API — только DTO

## 3. Naming Conventions

- `PascalCase` — классы, методы, свойства, события, namespace
- `camelCase` — локальные переменные, параметры
- `_camelCase` — приватные поля
- `I` — префикс интерфейсов (`IUserRepository`); абстрактные классы без префикса
- Суффикс `Async` для всех асинхронных методов
- Тест-класс: `{ClassUnderTest}Tests`; тест-метод: `{Method}_{Scenario}_{ExpectedResult}`
- URL: plural nouns, kebab-case (`/api/users`, `/api/user-workspaces/{id}/roles`)

## 4. Code Structure

- Use case folder: `Application/{Feature}/{Commands|Queries}/{Name}/` — Command, Handler, Validator, Response DTO в одной папке
- Entity: `sealed class`, private setters, фабричный метод `Create(...)`, приватный конструктор для EF Core
- Value Object: `sealed record`, самовалидация в конструкторе, equality по всем свойствам
- Aggregate Root: модификация дочерних сущностей только через корень; один репозиторий на агрегат; загрузка/сохранение целиком
- Domain Events: имена в прошедшем времени (`UserCreatedEvent`); собираются в агрегате, dispatch после persist

## 5. Dependency Rules

- Constructor injection; service locator запрещён
- Регистрация через extension-методы: `AddApplication()`, `AddInfrastructure()`, `AddPresentation()`
- Конфигурация через `IOptions<T>` / `IOptionsSnapshot<T>`
- Domain и Application определяют интерфейсы — Infrastructure реализует

## 6. Error Handling

- Специфичные исключения (`EntityNotFoundException`, `DomainValidationException`) наследуют от `DomainException`
- Запрещено ловить `Exception` без re-throw
- Маппинг исключений → HTTP-кодов в глобальном exception middleware
- Ошибки API в формате RFC 7807 ProblemDetails

## 7. Logging Rules

- Structured logging с message templates (не string interpolation)
- Формат: `logger.LogError(ex, "Failed to do X for {EntityId}", entityId)`
- `CorrelationId` header прокидывается через middleware

## 8. Security Requirements

- Все эндпоинты `[Authorize]` кроме auth-маршрутов
- Nullable reference types включены (`<Nullable>enable</Nullable>`)
- Guard clauses: `ArgumentNullException.ThrowIfNull()`, `ArgumentException.ThrowIfNullOrWhiteSpace()`
- Никогда не подавлять nullable warnings (`null!`) без комментария

## 9. Performance Requirements

- Все I/O-операции — async/await; запрещены `.Result` и `.Wait()`
- `CancellationToken` прокидывается через всю цепочку вызовов
- Если `await` не нужен — возвращать `Task` напрямую, без `async` keyword

## 10. Testing Requirements

- Фреймворк: xUnit; моки: Moq; ассерты: FluentAssertions (`.Should()`)
- Структура: Arrange-Act-Assert; один логический assert на тест
- `[Theory]` + `[InlineData]` для параметризованных кейсов
- Тесты изолированы — нет зависимости от состояния других тестов
- Integration tests: `Testcontainers` (PostgreSQL); API tests: `WebApplicationFactory<Program>`
- Покрытие по слоям: Domain (создание, валидация, VO equality), Application (handler-логика, валидация, маппинг), Infrastructure (CRUD, EF-конфигурации, миграции), API (статус-коды, формат ответа, авторизация, ошибки)

## 11. Documentation Requirements

- Контроллеры: `[ProducesResponseType]` на каждый Action
- Поиск: `POST /api/{entity}/search` с телом `{ query, sortBy, sortOrder, page, pageSize }` → `{ items, page, pageSize, totalCount }`

## 12. Code Review Rules

- Нет бизнес-логики в контроллерах/репозиториях
- Нет ссылок на `DbContext`/EF-типы в Application и Domain
- Нет мутабельных коллекций наружу — только `IReadOnlyCollection<T>`
- Нет циклических зависимостей между слоями
- `var` только когда тип очевиден из правой части; `sealed` на классах без наследования
- `record` для immutable DTO и value objects; pattern matching вместо if-else цепочек

## 13. Definition of Done

- Код соответствует Clean Architecture (направление зависимостей верно)
- SOLID-принципы соблюдены
- Тесты написаны до реализации (TDD), все проходят
- FluentValidation-валидатор на каждую команду
- Nullable warnings устранены
- Нет подавленных warning без комментариев

## 14. Exceptions

- Глагол в URL допускается только для `/api/auth/*`
- POST для поиска: `POST /api/{entity}/search`
