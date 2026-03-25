using Auth.Application;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.AuditLogs;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.AuditLogs;

public sealed class AuditBehaviorTests
{
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IAuditContext> _auditContext = new();
    private readonly Mock<ILogger<AuditBehavior<TestAuditCommand, TestAuditResponse>>> _logger = new();

    private AuditBehavior<TestAuditCommand, TestAuditResponse> CreateBehavior(AuthDbContext dbContext) =>
        new(_auditService.Object, _auditContext.Object, dbContext, _logger.Object);

    [Fact]
    public async Task Handle_NonCritical_CallsNextAndAuditService()
    {
        await using var dbContext = CreateDbContext();
        var behavior = CreateBehavior(dbContext);
        var command = new TestAuditCommand(AuditEntityType.User, AuditAction.Create, Guid.NewGuid(), Critical: false);
        var expectedResponse = new TestAuditResponse("ok");
        var nextCalled = false;

        var result = await behavior.Handle(
            command,
            () => { nextCalled = true; return Task.FromResult(expectedResponse); },
            CancellationToken.None);

        nextCalled.Should().BeTrue();
        result.Should().Be(expectedResponse);
        _auditService.Verify(x => x.LogAsync(
            command.EntityType,
            command.EntityId,
            command.Action,
            It.IsAny<Dictionary<string, object?>?>(),
            It.IsAny<AuditActor?>(),
            false,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonCritical_AuditFailure_StillReturnsResponse()
    {
        await using var dbContext = CreateDbContext();
        var behavior = CreateBehavior(dbContext);
        var command = new TestAuditCommand(AuditEntityType.User, AuditAction.Create, Guid.NewGuid(), Critical: false);
        var expectedResponse = new TestAuditResponse("ok");

        _auditService.Setup(x => x.LogAsync(
                It.IsAny<AuditEntityType>(), It.IsAny<Guid>(), It.IsAny<AuditAction>(),
                It.IsAny<Dictionary<string, object?>?>(), It.IsAny<AuditActor?>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Audit storage unavailable"));

        var result = await behavior.Handle(
            command,
            () => Task.FromResult(expectedResponse),
            CancellationToken.None);

        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_Critical_ThrowsBecauseInMemoryDoesNotSupportTransactions()
    {
        await using var dbContext = CreateDbContext();
        var behavior = CreateBehavior(dbContext);
        var command = new TestAuditCommand(AuditEntityType.User, AuditAction.Create, Guid.NewGuid(), Critical: true);
        var expectedResponse = new TestAuditResponse("ok");

        // InMemory provider does not support transactions, so BeginTransactionAsync throws.
        // This confirms the critical path attempts to start a transaction.
        var act = () => behavior.Handle(
            command,
            () => Task.FromResult(expectedResponse),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_UsesEntityIdFromAuditContext_WhenSet()
    {
        await using var dbContext = CreateDbContext();
        var behavior = CreateBehavior(dbContext);
        var overriddenId = Guid.NewGuid();
        var command = new TestAuditCommand(AuditEntityType.Role, AuditAction.Update, Guid.NewGuid(), Critical: false);

        _auditContext.Setup(x => x.EntityId).Returns(overriddenId);

        await behavior.Handle(
            command,
            () => Task.FromResult(new TestAuditResponse("ok")),
            CancellationToken.None);

        _auditService.Verify(x => x.LogAsync(
            command.EntityType,
            overriddenId,
            command.Action,
            It.IsAny<Dictionary<string, object?>?>(),
            It.IsAny<AuditActor?>(),
            false,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UsesDetailsFromAuditContext()
    {
        await using var dbContext = CreateDbContext();
        var behavior = CreateBehavior(dbContext);
        var command = new TestAuditCommand(AuditEntityType.Workspace, AuditAction.Patch, Guid.NewGuid(), Critical: false);
        var details = new Dictionary<string, object?> { ["field"] = "name", ["oldValue"] = "A", ["newValue"] = "B" };

        _auditContext.Setup(x => x.Details).Returns(details);

        await behavior.Handle(
            command,
            () => Task.FromResult(new TestAuditResponse("ok")),
            CancellationToken.None);

        _auditService.Verify(x => x.LogAsync(
            command.EntityType,
            command.EntityId,
            command.Action,
            details,
            It.IsAny<AuditActor?>(),
            false,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenResponseIsAuditActorProvider_PassesActor()
    {
        await using var dbContext = CreateDbContext();
        var behaviorWithActor = new AuditBehavior<TestAuditCommand, TestAuditActorResponse>(
            _auditService.Object, _auditContext.Object, dbContext,
            new Mock<ILogger<AuditBehavior<TestAuditCommand, TestAuditActorResponse>>>().Object);

        var command = new TestAuditCommand(AuditEntityType.User, AuditAction.Login, Guid.NewGuid(), Critical: false);
        var actorId = Guid.NewGuid();
        var expectedActor = new AuditActor(actorId, "john.doe", AuditActorType.User);
        var response = new TestAuditActorResponse("ok", expectedActor);

        await behaviorWithActor.Handle(
            command,
            () => Task.FromResult(response),
            CancellationToken.None);

        _auditService.Verify(x => x.LogAsync(
            command.EntityType,
            command.EntityId,
            command.Action,
            It.IsAny<Dictionary<string, object?>?>(),
            expectedActor,
            false,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

public sealed record TestAuditCommand(
    AuditEntityType EntityType,
    AuditAction Action,
    Guid EntityId,
    bool Critical) : IAuditable;

public sealed record TestAuditResponse(string Value);

public sealed record TestAuditActorResponse(string Value, AuditActor? Actor) : IAuditActorProvider
{
    public AuditActor? GetAuditActor() => Actor;
}
