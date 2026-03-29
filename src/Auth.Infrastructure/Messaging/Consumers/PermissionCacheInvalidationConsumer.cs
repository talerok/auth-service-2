using Auth.Application;
using Auth.Application.Messaging.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.Messaging.Consumers;

internal sealed class PermissionCacheInvalidationConsumer(
    IPermissionBitCache permissionBitCache,
    ILogger<PermissionCacheInvalidationConsumer> logger)
    : IConsumer<PermissionCreatedEvent>,
      IConsumer<PermissionUpdatedEvent>,
      IConsumer<PermissionDeletedEvent>
{
    public Task Consume(ConsumeContext<PermissionCreatedEvent> context) => InvalidateAsync(context.CancellationToken);

    public Task Consume(ConsumeContext<PermissionUpdatedEvent> context) => InvalidateAsync(context.CancellationToken);

    public Task Consume(ConsumeContext<PermissionDeletedEvent> context) => InvalidateAsync(context.CancellationToken);

    private async Task InvalidateAsync(CancellationToken ct)
    {
        logger.LogInformation("Permission changed, refreshing permission bit cache");
        await permissionBitCache.WarmupAsync(ct);
    }
}
