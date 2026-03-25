using Auth.Application;
using Auth.Application.Workspaces.Commands.CreateWorkspace;
using Auth.Domain;
using Auth.Infrastructure;
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
        var searchIndex = new Mock<ISearchIndexService>();
        searchIndex.Setup(x => x.IndexWorkspaceAsync(It.IsAny<WorkspaceDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var scopeManager = new Mock<IOpenIddictScopeManager>();
        var handler = new CreateWorkspaceCommandHandler(dbContext, searchIndex.Object, scopeManager.Object, new Mock<IAuditContext>().Object);

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

        searchIndex.Verify(x => x.IndexWorkspaceAsync(
            It.Is<WorkspaceDto>(d => d.Name == "Test Workspace"),
            It.IsAny<CancellationToken>()), Times.Once);

        scopeManager.Verify(x => x.CreateAsync(
            It.Is<OpenIddictScopeDescriptor>(d => d.Name == "ws:test-ws"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

}
