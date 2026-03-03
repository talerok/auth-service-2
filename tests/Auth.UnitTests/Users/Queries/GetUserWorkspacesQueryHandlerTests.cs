using Auth.Application;
using Auth.Application.Users.Queries.GetUserWorkspaces;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Users.Queries.GetUserWorkspaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Auth.UnitTests.Users.Queries;

public sealed class GetUserWorkspacesQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetUserWorkspacesQueryHandler(dbContext);

        var result = await handler.Handle(new GetUserWorkspacesQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenUserHasNoWorkspaces_ReturnsEmptyCollection()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var handler = new GetUserWorkspacesQueryHandler(dbContext);

        var result = await handler.Handle(new GetUserWorkspacesQuery(user.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenUserHasWorkspaceWithNoRoles_ReturnsWorkspaceWithEmptyRoleIds()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        var workspace = new Workspace { Name = "system", Description = "System workspace", IsSystem = false };
        dbContext.Users.Add(user);
        dbContext.Workspaces.Add(workspace);
        await dbContext.SaveChangesAsync();
        dbContext.UserWorkspaces.Add(new UserWorkspace { UserId = user.Id, WorkspaceId = workspace.Id });
        await dbContext.SaveChangesAsync();
        var handler = new GetUserWorkspacesQueryHandler(dbContext);

        var result = await handler.Handle(new GetUserWorkspacesQuery(user.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result!.Single().WorkSpaceId.Should().Be(workspace.Id);
        result.Single().RoleIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenUserHasWorkspacesWithRoles_ReturnsWorkspacesWithRoleIds()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        var workspace = new Workspace { Name = "system", Description = "System workspace", IsSystem = false };
        var role = new Role { Name = "editor", Description = "Editor" };
        dbContext.Users.Add(user);
        dbContext.Workspaces.Add(workspace);
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();
        var userWorkspace = new UserWorkspace { UserId = user.Id, WorkspaceId = workspace.Id };
        dbContext.UserWorkspaces.Add(userWorkspace);
        await dbContext.SaveChangesAsync();
        dbContext.UserWorkspaceRoles.Add(new UserWorkspaceRole { UserWorkspaceId = userWorkspace.Id, RoleId = role.Id });
        await dbContext.SaveChangesAsync();
        var handler = new GetUserWorkspacesQueryHandler(dbContext);

        var result = await handler.Handle(new GetUserWorkspacesQuery(user.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result!.Single().WorkSpaceId.Should().Be(workspace.Id);
        result.Single().RoleIds.Should().ContainSingle(id => id == role.Id);
    }

    [Fact]
    public async Task Handle_WhenUserHasMultipleWorkspaces_ReturnsAll()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        var ws1 = new Workspace { Name = "ws1", Description = "Workspace 1", IsSystem = false };
        var ws2 = new Workspace { Name = "ws2", Description = "Workspace 2", IsSystem = false };
        dbContext.Users.Add(user);
        dbContext.Workspaces.AddRange(ws1, ws2);
        await dbContext.SaveChangesAsync();
        dbContext.UserWorkspaces.AddRange(
            new UserWorkspace { UserId = user.Id, WorkspaceId = ws1.Id },
            new UserWorkspace { UserId = user.Id, WorkspaceId = ws2.Id });
        await dbContext.SaveChangesAsync();
        var handler = new GetUserWorkspacesQueryHandler(dbContext);

        var result = await handler.Handle(new GetUserWorkspacesQuery(user.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result!.Select(x => x.WorkSpaceId).Should().BeEquivalentTo(new[] { ws1.Id, ws2.Id });
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
