using System.Security.Claims;
using Auth.Application;
using Auth.Domain;
using Auth.Infrastructure.AuditLogs;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.AuditLogs;

public sealed class AuditServiceTests
{
    private readonly Mock<ISearchIndexService> _searchIndexService = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly Mock<ILogger<AuditService>> _logger = new();

    private AuditService CreateService(Infrastructure.AuthDbContext dbContext) =>
        new(dbContext, _searchIndexService.Object, _httpContextAccessor.Object, _logger.Object);

    [Fact]
    public async Task LogAsync_SavesEntryToDatabase()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var entityId = Guid.NewGuid();

        await service.LogAsync(
            AuditEntityType.User, entityId, AuditAction.Create,
            cancellationToken: CancellationToken.None);

        var entry = await dbContext.AuditLogEntries.SingleAsync();
        entry.EntityType.Should().Be(AuditEntityType.User);
        entry.EntityId.Should().Be(entityId);
        entry.Action.Should().Be(AuditAction.Create);
    }

    [Fact]
    public async Task LogAsync_WithExplicitActor_UsesProvidedActor()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var actorId = Guid.NewGuid();
        var actor = new AuditActor(actorId, "service-bot", AuditActorType.ServiceAccount);

        await service.LogAsync(
            AuditEntityType.Role, Guid.NewGuid(), AuditAction.Update,
            actor: actor, cancellationToken: CancellationToken.None);

        var entry = await dbContext.AuditLogEntries.SingleAsync();
        entry.ActorId.Should().Be(actorId);
        entry.ActorName.Should().Be("service-bot");
        entry.ActorType.Should().Be(AuditActorType.ServiceAccount);
    }

    [Fact]
    public async Task LogAsync_WithoutActor_ResolvesFromHttpContext()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", userId.ToString()),
                new Claim("name", "Jane Doe")
            ]))
        };
        _httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var service = CreateService(dbContext);

        await service.LogAsync(
            AuditEntityType.Workspace, Guid.NewGuid(), AuditAction.Create,
            cancellationToken: CancellationToken.None);

        var entry = await dbContext.AuditLogEntries.SingleAsync();
        entry.ActorId.Should().Be(userId);
        entry.ActorName.Should().Be("Jane Doe");
        entry.ActorType.Should().Be(AuditActorType.User);
    }

    [Fact]
    public async Task LogAsync_IndexesEntryToSearch()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var entityId = Guid.NewGuid();

        await service.LogAsync(
            AuditEntityType.Permission, entityId, AuditAction.SoftDelete,
            cancellationToken: CancellationToken.None);

        _searchIndexService.Verify(x => x.IndexAuditLogAsync(
            It.Is<AuditLogDto>(dto =>
                dto.EntityType == AuditLogDto.CamelCase(AuditEntityType.Permission) &&
                dto.EntityId == entityId &&
                dto.Action == AuditLogDto.CamelCase(AuditAction.SoftDelete)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogAsync_SearchIndexFails_DoesNotThrow()
    {
        await using var dbContext = CreateDbContext();
        _searchIndexService.Setup(x => x.IndexAuditLogAsync(
                It.IsAny<AuditLogDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("OpenSearch unavailable"));

        var service = CreateService(dbContext);
        var entityId = Guid.NewGuid();

        var act = () => service.LogAsync(
            AuditEntityType.Application, entityId, AuditAction.Create,
            cancellationToken: CancellationToken.None);

        await act.Should().NotThrowAsync();

        var entry = await dbContext.AuditLogEntries.SingleAsync();
        entry.EntityId.Should().Be(entityId);
    }

    [Fact]
    public async Task LogAsync_WithDetails_SavesDetails()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var details = new Dictionary<string, object?>
        {
            ["changedField"] = "email",
            ["oldValue"] = "old@example.com",
            ["newValue"] = "new@example.com"
        };

        await service.LogAsync(
            AuditEntityType.User, Guid.NewGuid(), AuditAction.Patch,
            details: details, cancellationToken: CancellationToken.None);

        var entry = await dbContext.AuditLogEntries.SingleAsync();
        entry.Details.Should().NotBeNull();
        entry.Details.Should().ContainKey("changedField").WhoseValue.Should().Be("email");
        entry.Details.Should().ContainKey("oldValue").WhoseValue.Should().Be("old@example.com");
        entry.Details.Should().ContainKey("newValue").WhoseValue.Should().Be("new@example.com");
    }
}
