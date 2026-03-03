using Auth.Application;
using Auth.Application.ApiClients.Queries.GetApiClientWorkspaces;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.ApiClients.Queries.GetApiClientWorkspaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Auth.UnitTests.ApiClients.Queries;

public sealed class GetApiClientWorkspacesQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenApiClientDoesNotExist_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetApiClientWorkspacesQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetApiClientWorkspacesQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenApiClientHasNoWorkspaces_ReturnsEmpty()
    {
        await using var dbContext = CreateDbContext();
        var apiClient = new ApiClient { Name = "Test", Description = "d", ClientId = "ac-1", IsActive = true };
        dbContext.ApiClients.Add(apiClient);
        await dbContext.SaveChangesAsync();
        var handler = new GetApiClientWorkspacesQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetApiClientWorkspacesQuery(apiClient.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenApiClientHasWorkspaces_ReturnsWithRoleIds()
    {
        await using var dbContext = CreateDbContext();
        var apiClient = new ApiClient { Name = "Test", Description = "d", ClientId = "ac-1", IsActive = true };
        var workspace = new Workspace { Name = "ws1", Description = "Workspace 1", IsSystem = false };
        var role = new Role { Name = "reader", Description = "Reader" };
        dbContext.ApiClients.Add(apiClient);
        dbContext.Workspaces.Add(workspace);
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();
        var acw = new ApiClientWorkspace { ApiClientId = apiClient.Id, WorkspaceId = workspace.Id };
        dbContext.ApiClientWorkspaces.Add(acw);
        await dbContext.SaveChangesAsync();
        dbContext.ApiClientWorkspaceRoles.Add(new ApiClientWorkspaceRole { ApiClientWorkspaceId = acw.Id, RoleId = role.Id });
        await dbContext.SaveChangesAsync();
        var handler = new GetApiClientWorkspacesQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetApiClientWorkspacesQuery(apiClient.Id), CancellationToken.None);

        result.Should().HaveCount(1);
        result!.Single().WorkspaceId.Should().Be(workspace.Id);
        result.Single().RoleIds.Should().ContainSingle(id => id == role.Id);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
