using Auth.Application.ServiceAccounts.Queries.GetServiceAccountWorkspaces;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.ServiceAccounts.Queries.GetServiceAccountWorkspaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.ServiceAccounts.Queries;

public sealed class GetServiceAccountWorkspacesQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenServiceAccountDoesNotExist_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetServiceAccountWorkspacesQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetServiceAccountWorkspacesQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenServiceAccountHasNoWorkspaces_ReturnsEmpty()
    {
        await using var dbContext = CreateDbContext();
        var serviceAccount = new Domain.ServiceAccount { Name = "Test", Description = "d", ClientId = "sa-1", IsActive = true };
        dbContext.ServiceAccounts.Add(serviceAccount);
        await dbContext.SaveChangesAsync();
        var handler = new GetServiceAccountWorkspacesQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetServiceAccountWorkspacesQuery(serviceAccount.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenServiceAccountHasWorkspaces_ReturnsWithRoleIds()
    {
        await using var dbContext = CreateDbContext();
        var serviceAccount = new Domain.ServiceAccount { Name = "Test", Description = "d", ClientId = "sa-1", IsActive = true };
        var workspace = new Workspace { Name = "ws1", Description = "Workspace 1", IsSystem = false };
        var role = new Role { Name = "reader", Description = "Reader" };
        dbContext.ServiceAccounts.Add(serviceAccount);
        dbContext.Workspaces.Add(workspace);
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();
        var saw = new ServiceAccountWorkspace { ServiceAccountId = serviceAccount.Id, WorkspaceId = workspace.Id };
        dbContext.ServiceAccountWorkspaces.Add(saw);
        await dbContext.SaveChangesAsync();
        dbContext.ServiceAccountWorkspaceRoles.Add(new ServiceAccountWorkspaceRole { ServiceAccountWorkspaceId = saw.Id, RoleId = role.Id });
        await dbContext.SaveChangesAsync();
        var handler = new GetServiceAccountWorkspacesQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetServiceAccountWorkspacesQuery(serviceAccount.Id), CancellationToken.None);

        result.Should().HaveCount(1);
        result!.Single().WorkspaceId.Should().Be(workspace.Id);
        result.Single().RoleIds.Should().ContainSingle(id => id == role.Id);
    }

}
