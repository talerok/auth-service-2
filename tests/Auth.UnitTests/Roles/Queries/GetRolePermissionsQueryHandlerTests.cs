using Auth.Application;
using Auth.Application.Roles.Queries.GetRolePermissions;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Roles.Queries.GetRolePermissions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Roles.Queries;

public sealed class GetRolePermissionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_RoleNotFound_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetRolePermissionsQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetRolePermissionsQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_RoleWithNoPermissions_ReturnsEmpty()
    {
        await using var dbContext = CreateDbContext();
        var role = new Role { Name = "Empty", Description = "No permissions" };
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();
        var handler = new GetRolePermissionsQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetRolePermissionsQuery(role.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_RoleWithPermissions_ReturnsAll()
    {
        await using var dbContext = CreateDbContext();
        var role = new Role { Name = "Admin", Description = "Administrator" };
        var perm1 = new Permission { Bit = 1, Code = "users.read", Description = "Read users", IsSystem = true };
        var perm2 = new Permission { Bit = 2, Code = "users.write", Description = "Write users", IsSystem = false };
        dbContext.Roles.Add(role);
        dbContext.Permissions.AddRange(perm1, perm2);
        await dbContext.SaveChangesAsync();
        dbContext.RolePermissions.AddRange(
            new RolePermission { RoleId = role.Id, PermissionId = perm1.Id },
            new RolePermission { RoleId = role.Id, PermissionId = perm2.Id });
        await dbContext.SaveChangesAsync();
        var handler = new GetRolePermissionsQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetRolePermissionsQuery(role.Id), CancellationToken.None);

        result.Should().HaveCount(2);
        result!.Select(x => x.Code).Should().BeEquivalentTo("users.read", "users.write");
    }

}
