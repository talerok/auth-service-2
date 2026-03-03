using Auth.Application;
using Auth.Application.Workspaces.Commands.SoftDeleteWorkspace;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Workspaces.Commands.SoftDeleteWorkspace;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Auth.UnitTests.Workspaces.Commands;

public sealed class SoftDeleteWorkspaceCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExistingWorkspace_SoftDeletesAndReturnsTrue()
    {
        await using var dbContext = CreateDbContext();
        var workspace = new Workspace { Name = "To Delete", Code = "del-ws", Description = "desc" };
        dbContext.Workspaces.Add(workspace);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        searchIndex.Setup(x => x.DeleteWorkspaceAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new SoftDeleteWorkspaceCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new SoftDeleteWorkspaceCommand(workspace.Id),
            CancellationToken.None);

        result.Should().BeTrue();
        var updated = await dbContext.Workspaces.IgnoreQueryFilters().FirstAsync(x => x.Id == workspace.Id);
        updated.DeletedAt.Should().NotBeNull();
        searchIndex.Verify(x => x.DeleteWorkspaceAsync(workspace.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistent_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new SoftDeleteWorkspaceCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new SoftDeleteWorkspaceCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SystemWorkspace_ThrowsForbidden()
    {
        await using var dbContext = CreateDbContext();
        var workspace = new Workspace { Name = "System", Code = "system", Description = "System workspace", IsSystem = true };
        dbContext.Workspaces.Add(workspace);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new SoftDeleteWorkspaceCommandHandler(dbContext, searchIndex.Object);

        var act = () => handler.Handle(
            new SoftDeleteWorkspaceCommand(workspace.Id),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AuthException>();
        ex.Which.Code.Should().Be(AuthErrorCatalog.SystemWorkspaceDeleteForbidden);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
