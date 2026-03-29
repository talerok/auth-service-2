using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Domain;
using Microsoft.AspNetCore.Http;

namespace Auth.Infrastructure.AuditLogs;

internal sealed class AuditService(
    AuthDbContext dbContext,
    IEventBus eventBus,
    IHttpContextAccessor httpContextAccessor) : IAuditService
{
    public async Task LogAsync(
        AuditEntityType entityType,
        Guid entityId,
        AuditAction action,
        Dictionary<string, object?>? details = null,
        AuditActor? actor = null,
        bool critical = false,
        CancellationToken cancellationToken = default)
    {
        var httpContext = httpContextAccessor.HttpContext;

        Guid? actorId;
        string? actorName;
        AuditActorType actorType;

        if (actor is not null)
        {
            actorId = actor.Id;
            actorName = actor.Name;
            actorType = actor.Type;
        }
        else
        {
            actorId = ResolveActorId(httpContext);
            actorName = ResolveActorName(httpContext);
            actorType = AuditActorType.User;
        }

        var entry = new AuditLogEntry
        {
            ActorId = actorId,
            ActorName = actorName,
            ActorType = actorType,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Details = details,
            IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext?.Request.Headers.UserAgent.FirstOrDefault()?.Truncate(500),
            CorrelationId = httpContext?.Items["CorrelationId"]?.ToString()
        };

        dbContext.AuditLogEntries.Add(entry);

        await eventBus.PublishAsync(new IndexAuditLogRequested { AuditLogEntryId = entry.Id }, cancellationToken);
    }

    private static Guid? ResolveActorId(HttpContext? httpContext)
    {
        var sub = httpContext?.User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private static string? ResolveActorName(HttpContext? httpContext)
    {
        return httpContext?.User.FindFirst("name")?.Value
            ?? httpContext?.User.FindFirst("preferred_username")?.Value;
    }

}

internal static class StringExtensions
{
    public static string? Truncate(this string? value, int maxLength) =>
        value is not null && value.Length > maxLength ? value[..maxLength] : value;
}
