using Auth.Application;
using Auth.Application.Messaging;
using Auth.Application.Workspaces.Commands.PatchWorkspace;
using Auth.Domain;
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
        var eventBus = new Mock<IEventBus>();
        var handler = new PatchWorkspaceCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new PatchWorkspaceCommand(workspace.Id, "Patched", default, default),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Patched");
        result.Code.Should().Be("orig-code");
        result.Description.Should().Be("Orig desc");

        eventBus.Verify(x => x.PublishAsync(It.IsAny<IIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_NonExistent_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var eventBus = new Mock<IEventBus>();
        var handler = new PatchWorkspaceCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new PatchWorkspaceCommand(Guid.NewGuid(), "Name", default, default),
            CancellationToken.None);

        result.Should().BeNull();
    }

}
