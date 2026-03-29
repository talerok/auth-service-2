using Auth.Application;
using Auth.Application.Messaging.Commands;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.Messaging.Consumers;

internal sealed class IndexAuditLogConsumer(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    ILogger<IndexAuditLogConsumer> logger) : IConsumer<IndexAuditLogRequested>
{
    public async Task Consume(ConsumeContext<IndexAuditLogRequested> context)
    {
        var ct = context.CancellationToken;

        var entry = await dbContext.AuditLogEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == context.Message.AuditLogEntryId, ct);

        if (entry is null)
        {
            logger.LogWarning("Audit log entry {AuditLogEntryId} not found, skipping indexing", context.Message.AuditLogEntryId);
            return;
        }

        var dto = new AuditLogDto(
            entry.Id, entry.Timestamp, entry.ActorId, entry.ActorName,
            AuditLogDto.CamelCase(entry.ActorType),
            AuditLogDto.CamelCase(entry.EntityType),
            entry.EntityId,
            AuditLogDto.CamelCase(entry.Action),
            entry.Details, entry.IpAddress, entry.UserAgent, entry.CorrelationId);

        await searchIndexService.IndexAuditLogAsync(dto, ct);
    }
}

internal sealed class IndexAuditLogConsumerDefinition : ConsumerDefinition<IndexAuditLogConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<IndexAuditLogConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Exponential(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)));
    }
}
