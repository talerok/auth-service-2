# Messaging & Caching

## Архитектура

Сервис использует **MassTransit** с транспортом **RabbitMQ** для асинхронной обработки:

- OpenSearch индексация (entities + audit logs)
- OTP delivery (email/SMS)
- Cache invalidation (permission bitmask)

**Transactional outbox** (EF Core) гарантирует атомарность: события сохраняются в той же транзакции, что и бизнес-данные. При падении RabbitMQ сообщения доставляются после восстановления.

**Redis** используется как distributed cache для горизонтального масштабирования (2+ инстанса).

## Clean Architecture

```
Auth.Application     — IEventBus, IIntegrationEvent, event/command records
Auth.Infrastructure  — MassTransitEventBus, consumers, Redis-backed caches
```

Application layer не зависит от MassTransit/Redis. Handlers инжектят `IEventBus`:

```csharp
await eventBus.PublishAsync(new UserCreatedEvent { ... }, cancellationToken);
await dbContext.SaveChangesAsync(cancellationToken);  // outbox delivers atomically
```

## Domain Events

Публичные события для внешних потребителей. MassTransit Publish = fanout (все подписчики получают).

| Event | Поля | Источник |
|---|---|---|
| `UserCreatedEvent` | UserId, Username, Email | CreateUser |
| `UserUpdatedEvent` | UserId, ChangedFields[] | UpdateUser, PatchUser |
| `UserDeletedEvent` | UserId | SoftDeleteUser |
| `UserBlockedEvent` | UserId | BlockUser |
| `UserAuthenticatedEvent` | UserId, AuthMethod, IpAddress?, ClientId? | Login, MFA, JWT Bearer, LDAP |
| `RoleCreatedEvent` | RoleId, Name | CreateRole |
| `RoleUpdatedEvent` | RoleId, ChangedFields[] | UpdateRole, PatchRole |
| `RoleDeletedEvent` | RoleId | SoftDeleteRole |
| `PermissionCreatedEvent` | PermissionId, Code, Domain, Bit | CreatePermission |
| `PermissionUpdatedEvent` | PermissionId, Code | UpdatePermission, PatchPermission |
| `PermissionDeletedEvent` | PermissionId, Code | SoftDeletePermission |
| `WorkspaceCreatedEvent` | WorkspaceId, Code | CreateWorkspace |
| `WorkspaceUpdatedEvent` | WorkspaceId | UpdateWorkspace, PatchWorkspace |
| `WorkspaceDeletedEvent` | WorkspaceId | SoftDeleteWorkspace |

Все события наследуют `IntegrationEventBase` (EventId, Timestamp, CorrelationId).

### Какие entities получают domain events

| Entity | Domain Events | Причина |
|---|---|---|
| User | Created, Updated, Deleted, Blocked, Authenticated | Ядро IAM — внешние системы подписываются на изменения пользователей |
| Role | Created, Updated, Deleted | RBAC — внешние системы синхронизируют роли |
| Permission | Created, Updated, Deleted | Bitmask cache invalidation + внешние системы |
| Workspace | Created, Updated, Deleted | Multi-tenancy — внешние системы создают matching структуры |
| Application | — | Внутренняя сущность OAuth, внешним потребителям не нужна |
| ServiceAccount | — | Внутренняя сущность OAuth |
| NotificationTemplate | — | Внутренняя сущность |

Все entities получают `IndexEntityRequested` для OpenSearch индексации.

## Internal Commands

Внутренние команды для consumers внутри сервиса. Competing consumers (одна очередь, один обработчик).

| Command | Поля | Назначение |
|---|---|---|
| `IndexEntityRequested` | EntityType, EntityId, Operation (Index/Delete) | Асинхронная OpenSearch индексация |
| `IndexAuditLogRequested` | AuditLogEntryId | Индексация audit log в OpenSearch |
| `DeliverOtpRequested` | ChallengeId | Доставка OTP по email/SMS |

## Consumers

### IndexEntityConsumer

Потребляет `IndexEntityRequested`. Загружает entity из БД, маппит в DTO, вызывает `ISearchIndexService`.

Поддерживаемые типы: User, Role, Permission, Workspace, Application, ServiceAccount, NotificationTemplate.

Retry: 5x exponential backoff (1s → 60s).

### IndexAuditLogConsumer

Потребляет `IndexAuditLogRequested`. Загружает `AuditLogEntry` из БД, индексирует в OpenSearch.

Retry: 3x exponential backoff (1s → 30s).

### DeliverOtpConsumer

Потребляет `DeliverOtpRequested`. Загружает challenge из БД, расшифровывает OTP, отправляет email/SMS через gateway.

Notification templates кешируются через `IDistributedCache` (TTL 5 мин).

Retry: 3x exponential backoff (1s → 30s).

### DeliverOtpFaultConsumer

Потребляет `Fault<DeliverOtpRequested>` — автоматически публикуется MassTransit после исчерпания retry. Обновляет `TwoFactorChallenge.DeliveryStatus` → `DELIVERY_FAILED` в БД и логирует ошибку.

### PermissionCacheInvalidationConsumer

Потребляет `PermissionCreatedEvent`, `PermissionUpdatedEvent`, `PermissionDeletedEvent`. Вызывает `IPermissionBitCache.WarmupAsync()`.

Fanout: все инстансы получают событие и обновляют свой кеш.

## Redis Caching

### PermissionBitCache

Double-layer cache: in-process volatile Dictionary + Redis backup.

- `WarmupAsync`: загрузка из БД, запись в Redis (TTL 10 мин) и in-process
- `TryGetBit`: чтение только из in-process Dictionary (hot path без I/O)
- Инвалидация: через `PermissionCacheInvalidationConsumer` (fanout на все инстансы)

### CorsOriginService

Volatile in-process HashSet → Redis → DB.

- TTL: 5 мин в Redis
- Инвалидация: при CRUD операциях с Applications

## Transactional Outbox

MassTransit EF Core outbox сохраняет сообщения в таблице `OutboxMessage` в той же транзакции:

```
Handler: eventBus.PublishAsync(event) → outbox table
Handler: dbContext.SaveChangesAsync() → entity + outbox in same tx
MassTransit: OutboxDeliveryService polls → delivers to RabbitMQ
```

Таблицы: `InboxState`, `OutboxState`, `OutboxMessage` (миграция `AddMassTransitOutbox`).

## Добавление нового события

1. Создать record в `src/Auth.Application/Messaging/Events/`, унаследовав от `IntegrationEventBase`:

```csharp
public sealed record MyNewEvent : IntegrationEventBase
{
    public required Guid EntityId { get; init; }
}
```

2. В handler добавить publish **до** `SaveChangesAsync`:

```csharp
await eventBus.PublishAsync(new MyNewEvent { EntityId = entity.Id }, cancellationToken);
await dbContext.SaveChangesAsync(cancellationToken);
```

3. (Опционально) Создать consumer в `src/Auth.Infrastructure/Messaging/Consumers/` и зарегистрировать в `ServiceCollectionExtensions.cs`:

```csharp
x.AddConsumer<MyNewConsumer, MyNewConsumerDefinition>();
```

## Environment Variables

### RabbitMQ

| Variable | Default | Description |
|---|---|---|
| `Integration__RabbitMq__Host` | localhost | Хост RabbitMQ |
| `Integration__RabbitMq__Port` | 5672 | Порт |
| `Integration__RabbitMq__VirtualHost` | / | Virtual host |
| `Integration__RabbitMq__Username` | guest | Логин |
| `Integration__RabbitMq__Password` | guest | Пароль |

### Redis

| Variable | Default | Description |
|---|---|---|
| `Integration__Redis__ConnectionString` | localhost:6379 | Connection string Redis |

## Event Ordering & Idempotency

**Ordering не гарантируется.** MassTransit + RabbitMQ не обеспечивают строгий порядок доставки событий. Причины:

- Outbox delivery service опрашивает таблицу без гарантий FIFO
- Retry и redelivery меняют порядок
- Competing consumers обрабатывают параллельно

**Контракт для внешних потребителей:**

1. Каждое событие содержит `Timestamp` (UTC) — используйте для reconciliation
2. Каждое событие содержит `EventId` (GUID) — используйте для дедупликации
3. Потребители **должны быть идемпотентными** — повторная обработка того же события не должна вызывать side effects
4. Не полагайтесь на порядок `UserCreatedEvent` → `UserUpdatedEvent` — они могут прийти в обратном порядке

**Гарантия доставки:** at-least-once (transactional outbox + retry). Exactly-once — ответственность потребителя через `EventId`.

## Горизонтальное масштабирование

При запуске 2+ инстансов:

- **Internal commands** (IndexEntity, DeliverOtp): competing consumers — одно сообщение обрабатывается одним инстансом
- **Domain events** (UserCreated, PermissionDeleted): fanout — все инстансы получают событие
- **PermissionBitCache**: инвалидируется на всех инстансах через `PermissionCacheInvalidationConsumer`
- **CorsOriginService**: Redis как shared cache, in-process cache обновляется по TTL
- **Outbox delivery**: `OutboxDeliveryService` использует row-level locking — безопасно для concurrent инстансов
