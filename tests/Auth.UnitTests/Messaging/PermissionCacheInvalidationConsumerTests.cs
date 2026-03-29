using Auth.Application;
using Auth.Application.Messaging.Events;
using Auth.Infrastructure.Messaging.Consumers;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Auth.UnitTests.Messaging;

public sealed class PermissionCacheInvalidationConsumerTests
{
    private readonly Mock<IPermissionBitCache> _cache = new();

    private PermissionCacheInvalidationConsumer CreateConsumer() =>
        new(_cache.Object, NullLogger<PermissionCacheInvalidationConsumer>.Instance);

    [Theory]
    [InlineData(nameof(PermissionCreatedEvent))]
    [InlineData(nameof(PermissionUpdatedEvent))]
    [InlineData(nameof(PermissionDeletedEvent))]
    public async Task Consume_PermissionEvent_CallsWarmup(string eventType)
    {
        var consumer = CreateConsumer();

        switch (eventType)
        {
            case nameof(PermissionCreatedEvent):
                var created = new Mock<ConsumeContext<PermissionCreatedEvent>>();
                created.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
                await consumer.Consume(created.Object);
                break;
            case nameof(PermissionUpdatedEvent):
                var updated = new Mock<ConsumeContext<PermissionUpdatedEvent>>();
                updated.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
                await consumer.Consume(updated.Object);
                break;
            case nameof(PermissionDeletedEvent):
                var deleted = new Mock<ConsumeContext<PermissionDeletedEvent>>();
                deleted.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
                await consumer.Consume(deleted.Object);
                break;
        }

        _cache.Verify(x => x.WarmupAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
