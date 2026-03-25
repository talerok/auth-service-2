using Auth.Application;
using Auth.Application.Workspaces.Commands.SoftDeleteWorkspace;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Workspaces.Commands.SoftDeleteWorkspace;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenIddict.Abstractions;
using static Auth.UnitTests.TestDbContextFactory;

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
        var scopeManager = new Mock<IOpenIddictScopeManager>();
        scopeManager.Setup(x => x.FindByNameAsync("ws:del-ws", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());
        var handler = new SoftDeleteWorkspaceCommandHandler(dbContext, searchIndex.Object, scopeManager.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new SoftDeleteWorkspaceCommand(workspace.Id),
            CancellationToken.None);

        result.Should().BeTrue();
        var updated = await dbContext.Workspaces.IgnoreQueryFilters().FirstAsync(x => x.Id == workspace.Id);
        updated.DeletedAt.Should().NotBeNull();
        searchIndex.Verify(x => x.DeleteWorkspaceAsync(workspace.Id, It.IsAny<CancellationToken>()), Times.Once);
        scopeManager.Verify(x => x.DeleteAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistent_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var scopeManager = new Mock<IOpenIddictScopeManager>();
        var handler = new SoftDeleteWorkspaceCommandHandler(dbContext, searchIndex.Object, scopeManager.Object, new Mock<IAuditContext>().Object);

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
        var scopeManager = new Mock<IOpenIddictScopeManager>();
        var handler = new SoftDeleteWorkspaceCommandHandler(dbContext, searchIndex.Object, scopeManager.Object, new Mock<IAuditContext>().Object);

        var act = () => handler.Handle(
            new SoftDeleteWorkspaceCommand(workspace.Id),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AuthException>();
        ex.Which.Code.Should().Be(AuthErrorCatalog.SystemWorkspaceDeleteForbidden);
    }

}
