using Auth.Application;
using Auth.Application.Users.Queries.ExportUsers;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Users.Queries.ExportUsers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Users.Queries;

public sealed class ExportUsersQueryHandlerTests
{
    [Fact]
    public async Task Export_ReturnsUsersWithWorkspaceAndRoleCodes()
    {
        await using var dbContext = CreateDbContext();
        var workspace = new Workspace { Name = "Main", Code = "main", Description = "Main workspace" };
        var role = new Role { Name = "Admin", Code = "admin", Description = "Administrator" };
        dbContext.Workspaces.Add(workspace);
        dbContext.Roles.Add(role);
        var user = new User { Username = "john", FullName = "John Doe", Email = "john@test.com", PasswordHash = "hash" };
        dbContext.Users.Add(user);
        var uw = new UserWorkspace { UserId = user.Id, WorkspaceId = workspace.Id };
        dbContext.UserWorkspaces.Add(uw);
        dbContext.UserWorkspaceRoles.Add(new UserWorkspaceRole { UserWorkspaceId = uw.Id, RoleId = role.Id });
        await dbContext.SaveChangesAsync();
        var handler = new ExportUsersQueryHandler(dbContext);

        var result = await handler.Handle(new ExportUsersQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        var exported = result.Single();
        exported.Username.Should().Be("john");
        exported.FullName.Should().Be("John Doe");
        exported.Email.Should().Be("john@test.com");
        exported.Workspaces.Should().HaveCount(1);
        exported.Workspaces.Single().WorkspaceCode.Should().Be("main");
        exported.Workspaces.Single().RoleCodes.Should().ContainSingle("admin");
    }

    [Fact]
    public async Task Export_ReturnsUsersWithIdentitySourceLinks()
    {
        await using var dbContext = CreateDbContext();
        var idSource = new IdentitySource { Name = "Corp LDAP", Code = "corp-ldap", DisplayName = "Corporate LDAP", Type = IdentitySourceType.Ldap };
        dbContext.IdentitySources.Add(idSource);
        var user = new User { Username = "jane", FullName = "Jane Doe", Email = "jane@test.com", PasswordHash = "hash" };
        dbContext.Users.Add(user);
        dbContext.IdentitySourceLinks.Add(new IdentitySourceLink { UserId = user.Id, IdentitySourceId = idSource.Id, ExternalIdentity = "uid=jane,ou=users" });
        await dbContext.SaveChangesAsync();
        var handler = new ExportUsersQueryHandler(dbContext);

        var result = await handler.Handle(new ExportUsersQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        var exported = result.Single();
        exported.IdentitySources.Should().HaveCount(1);
        exported.IdentitySources.Single().IdentitySourceCode.Should().Be("corp-ldap");
        exported.IdentitySources.Single().ExternalIdentity.Should().Be("uid=jane,ou=users");
    }

    [Fact]
    public async Task Export_ExcludesSoftDeleted()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.AddRange(
            new User { Username = "active", FullName = "Active", Email = "a@test.com", PasswordHash = "hash" },
            new User { Username = "deleted", FullName = "Deleted", Email = "d@test.com", PasswordHash = "hash", DeletedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();
        var handler = new ExportUsersQueryHandler(dbContext);

        var result = await handler.Handle(new ExportUsersQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result.Single().Username.Should().Be("active");
    }

    [Fact]
    public async Task Export_OrdersByUsername()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.AddRange(
            new User { Username = "zebra", FullName = "Z", Email = "z@test.com", PasswordHash = "hash" },
            new User { Username = "alpha", FullName = "A", Email = "a@test.com", PasswordHash = "hash" });
        await dbContext.SaveChangesAsync();
        var handler = new ExportUsersQueryHandler(dbContext);

        var result = await handler.Handle(new ExportUsersQuery(), CancellationToken.None);

        result.Select(x => x.Username).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Export_ReturnsEmptyWhenNoUsers()
    {
        await using var dbContext = CreateDbContext();
        var handler = new ExportUsersQueryHandler(dbContext);

        var result = await handler.Handle(new ExportUsersQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Export_ExcludesPasswordHash()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new User { Username = "user", FullName = "User", Email = "u@test.com", PasswordHash = "secret-hash" });
        await dbContext.SaveChangesAsync();
        var handler = new ExportUsersQueryHandler(dbContext);

        var result = await handler.Handle(new ExportUsersQuery(), CancellationToken.None);

        var exported = result.Single();
        var type = exported.GetType();
        type.GetProperties().Select(p => p.Name).Should().NotContain("PasswordHash");
    }

}
