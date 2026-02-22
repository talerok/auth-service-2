# Testing Requirements

Version: 1  
Status: draft  
Last updated: 2026-02-18

---

## 1. Testing Strategy

- Подход: TDD (Red-Green-Refactor) — тесты пишутся до реализации
- Фреймворк: xUnit; моки: Moq; ассерты: FluentAssertions (`.Should()`)
- Структура каждого теста: Arrange-Act-Assert
- Один логический assert на тест (несколько `.Should()` на одном объекте допустимы)
- Тесты изолированы — запрещена зависимость от состояния других тестов

---

## 2. Test Levels

### Unit Tests

- Domain: создание сущностей, валидация инвариантов, equality Value Objects, поведение агрегатов
- Application: логика Handlers, правила FluentValidation, корректность маппинга в DTO
- Моки для всех внешних зависимостей через Moq

### Integration Tests

- Infrastructure: CRUD-операции репозиториев, EF Core конфигурации, применение миграций
- База данных: PostgreSQL через `Testcontainers` (реальный контейнер на каждый тестовый прогон)

### Contract Tests

- Не применяются на текущем этапе (single-service)

### End-to-End Tests

- API: полный HTTP-pipeline через `WebApplicationFactory<Program>` с тестовой БД
- Проверки: статус-коды, формат ответа, авторизация (`[Authorize]`), обработка ошибок (ProblemDetails)
- HTTP-коды: GET → 200, POST → 201, PUT → 200, DELETE → 204

---

## 3. Coverage Requirements

- Domain-слой: 100% покрытие бизнес-правил и инвариантов
- Application-слой: каждый Handler и Validator покрыт тестами
- Infrastructure: основные CRUD-сценарии и edge cases
- API: happy path + error cases для каждого endpoint
- FluentValidation-валидатор обязателен для каждой команды и покрыт тестами

---

## 4. Test Structure & Organization

```
tests/
  Auth.Domain.UnitTests/              # Entity/VO/Aggregate поведение
  Auth.Application.UnitTests/         # Command/Query Handler логика
  Auth.Infrastructure.IntegrationTests/  # EF Core + реальная БД
  Auth.API.IntegrationTests/          # Полный HTTP pipeline
```

- Тест-проект зеркалит структуру основного проекта
- `[Theory]` + `[InlineData]` для параметризованных кейсов

---

## 5. Test Naming Conventions

- Тест-класс: `{ClassUnderTest}Tests` (например, `CreateUserCommandHandlerTests`)
- Тест-метод: `{Method}_{Scenario}_{ExpectedResult}`
- Примеры: `Handle_ValidCommand_ReturnsCreatedUser`, `Handle_DuplicateEmail_ThrowsDomainException`

---

## 6. Mocking & Test Doubles

- Moq для всех зависимостей в unit-тестах (`new Mock<IUserRepository>()`)
- Verify вызовов: `repository.Verify(r => r.AddAsync(...), Times.Once)`
- Integration-тесты используют реальные реализации (Testcontainers, WebApplicationFactory) — без моков
- `CancellationToken.None` в тестовых вызовах Handler

---
