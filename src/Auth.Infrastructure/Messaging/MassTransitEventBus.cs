using Auth.Application;
using MassTransit;
using Microsoft.AspNetCore.Http;

namespace Auth.Infrastructure.Messaging;

internal sealed class MassTransitEventBus(
    IPublishEndpoint publishEndpoint,
    IHttpContextAccessor httpContextAccessor) : IEventBus
{
    public Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class, Application.Messaging.IIntegrationEvent
    {
        var correlationId = httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString();

        if (correlationId is null)
            return publishEndpoint.Publish(message, ct);

        return publishEndpoint.Publish(message, ctx =>
        {
            if (Guid.TryParse(correlationId, out var id))
                ctx.CorrelationId = id;
            ctx.Headers.Set("X-Correlation-Id", correlationId);
        }, ct);
    }
}
