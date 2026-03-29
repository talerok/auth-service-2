using Auth.Application;
using Auth.Application.Messaging;
using Auth.Application.Workspaces.Commands.CreateWorkspace;
using Auth.Domain;
using Auth.Infrastructure.Workspaces.Commands.CreateWorkspace;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenIddict.Abstractions;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Workspaces.Commands;

public sealed class CreateWorkspaceCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_CreatesWorkspaceAndIndexes()
    {
        await using var dbContext = CreateDbContext();
        var eventBus = new Mock<IEventBus>();
        var scopeManager = new Mock<IOpenIddictScopeManager>();
        var handler = new CreateWorkspaceCommandHandler(dbContext, eventBus.Object, scopeManager.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new CreateWorkspaceCommand("Test Workspace", "test-ws", "A test workspace"),
            CancellationToken.None);

        result.Name.Should().Be("Test Workspace");
        result.Code.Should().Be("test-ws");
        result.Description.Should().Be("A test workspace");
        result.IsSystem.Should().BeFalse();

        var saved = await dbContext.Workspaces.FirstAsync();
        saved.Name.Should().Be("Test Workspace");
        saved.Code.Should().Be("test-ws");

        eventBus.Verify(x => x.PublishAsync(It.IsAny<IIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        scopeManager.Verify(x => x.CreateAsync(
            It.Is<OpenIddictScopeDescriptor>(d => d.Name == "ws:test-ws"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

}
