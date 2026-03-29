using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Messaging.Consumers;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Messaging;

public sealed class IndexAuditLogConsumerTests
{
    private readonly Mock<ISearchIndexService> _searchIndex = new();

    private IndexAuditLogConsumer CreateConsumer(AuthDbContext dbContext) =>
        new(dbContext, _searchIndex.Object, NullLogger<IndexAuditLogConsumer>.Instance);

    private static Mock<ConsumeContext<IndexAuditLogRequested>> CreateContext(Guid auditLogEntryId)
    {
        var context = new Mock<ConsumeContext<IndexAuditLogRequested>>();
        context.Setup(x => x.Message).Returns(new IndexAuditLogRequested { AuditLogEntryId = auditLogEntryId });
        context.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
        return context;
    }

    [Fact]
    public async Task Consume_EntryExists_IndexesAuditLog()
    {
        await using var dbContext = CreateDbContext();
        var entry = new AuditLogEntry
        {
            EntityType = AuditEntityType.User,
            EntityId = Guid.NewGuid(),
            Action = AuditAction.Create,
            ActorType = AuditActorType.User
        };
        dbContext.AuditLogEntries.Add(entry);
        await dbContext.SaveChangesAsync();

        var consumer = CreateConsumer(dbContext);
        var context = CreateContext(entry.Id);

        await consumer.Consume(context.Object);

        _searchIndex.Verify(x => x.IndexAuditLogAsync(
            It.Is<AuditLogDto>(dto => dto.Id == entry.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_EntryNotFound_SkipsIndexing()
    {
        await using var dbContext = CreateDbContext();
        var consumer = CreateConsumer(dbContext);
        var context = CreateContext(Guid.NewGuid());

        await consumer.Consume(context.Object);

        _searchIndex.Verify(x => x.IndexAuditLogAsync(
            It.IsAny<AuditLogDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
