using Auth.Application;
using Auth.Application.Messaging.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.Messaging.Consumers;

internal sealed class CorsOriginCacheInvalidationConsumer(
    ICorsOriginService corsOriginService,
    ILogger<CorsOriginCacheInvalidationConsumer> logger)
    : IConsumer<CorsOriginsChangedEvent>
{
    public async Task Consume(ConsumeContext<CorsOriginsChangedEvent> context)
    {
        logger.LogInformation("CORS origins changed, refreshing cache");
        await corsOriginService.WarmupAsync(context.CancellationToken);
    }
}
