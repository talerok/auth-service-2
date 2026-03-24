using Auth.Application;
using Auth.Application.Workspaces.Commands.PatchWorkspace;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Workspaces.Commands.PatchWorkspace;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Workspaces.Commands;

public sealed class PatchWorkspaceCommandHandlerTests
{
    [Fact]
    public async Task Handle_PatchName_OnlyUpdatesName()
    {
        await using var dbContext = CreateDbContext();
        var workspace = new Workspace { Name = "Original", Code = "orig-code", Description = "Orig desc" };
        dbContext.Workspaces.Add(workspace);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        searchIndex.Setup(x => x.IndexWorkspaceAsync(It.IsAny<WorkspaceDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new PatchWorkspaceCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new PatchWorkspaceCommand(workspace.Id, "Patched", null, null),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Patched");
        result.Code.Should().Be("orig-code");
        result.Description.Should().Be("Orig desc");

        searchIndex.Verify(x => x.IndexWorkspaceAsync(
            It.Is<WorkspaceDto>(d => d.Name == "Patched"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistent_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new PatchWorkspaceCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new PatchWorkspaceCommand(Guid.NewGuid(), "Name", null, null),
            CancellationToken.None);

        result.Should().BeNull();
    }

}
