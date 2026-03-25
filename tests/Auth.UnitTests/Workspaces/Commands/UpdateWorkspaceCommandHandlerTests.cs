using Auth.Application;
using Auth.Application.Workspaces.Commands.UpdateWorkspace;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Workspaces.Commands.UpdateWorkspace;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Workspaces.Commands;

public sealed class UpdateWorkspaceCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExistingWorkspace_UpdatesAndReturns()
    {
        await using var dbContext = CreateDbContext();
        var workspace = new Workspace { Name = "Old", Code = "old-code", Description = "Old desc" };
        dbContext.Workspaces.Add(workspace);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        searchIndex.Setup(x => x.IndexWorkspaceAsync(It.IsAny<WorkspaceDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new UpdateWorkspaceCommandHandler(dbContext, searchIndex.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new UpdateWorkspaceCommand(workspace.Id, "New", "new-code", "New desc"),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("New");
        result.Code.Should().Be("new-code");
        result.Description.Should().Be("New desc");

        searchIndex.Verify(x => x.IndexWorkspaceAsync(
            It.Is<WorkspaceDto>(d => d.Name == "New"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistent_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new UpdateWorkspaceCommandHandler(dbContext, searchIndex.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new UpdateWorkspaceCommand(Guid.NewGuid(), "Name", "code", "Desc"),
            CancellationToken.None);

        result.Should().BeNull();
    }

}
