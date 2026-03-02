using Auth.Application;
using Auth.Domain;
using Auth.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Auth.UnitTests;

public sealed class RoleServiceTests
{
    [Fact]
    public async Task GetPermissionsAsync_WhenRoleDoesNotExist_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.GetPermissionsAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPermissionsAsync_WhenRoleExistsWithNoPermissions_ReturnsEmptyCollection()
    {
        await using var dbContext = CreateDbContext();
        var role = new Role { Name = "admin", Description = "Administrator" };
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.GetPermissionsAsync(role.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPermissionsAsync_WhenRoleHasPermissions_ReturnsAllPermissions()
    {
        await using var dbContext = CreateDbContext();
        var role = new Role { Name = "admin", Description = "Administrator" };
        var permission = new Permission { Bit = 0, Code = "system.users.view", Description = "View users", IsSystem = true };
        dbContext.Roles.Add(role);
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync();
        dbContext.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.GetPermissionsAsync(role.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result!.Single().Id.Should().Be(permission.Id);
        result.Single().Code.Should().Be("system.users.view");
        result.Single().Bit.Should().Be(0);
        result.Single().IsSystem.Should().BeTrue();
    }

    [Fact]
    public async Task GetPermissionsAsync_WhenRoleHasMultiplePermissions_ReturnsAllOfThem()
    {
        await using var dbContext = CreateDbContext();
        var role = new Role { Name = "admin", Description = "Administrator" };
        var p1 = new Permission { Bit = 0, Code = "system.users.view", Description = "View users", IsSystem = true };
        var p2 = new Permission { Bit = 1, Code = "system.users.create", Description = "Create users", IsSystem = true };
        dbContext.Roles.Add(role);
        dbContext.Permissions.AddRange(p1, p2);
        await dbContext.SaveChangesAsync();
        dbContext.RolePermissions.AddRange(
            new RolePermission { RoleId = role.Id, PermissionId = p1.Id },
            new RolePermission { RoleId = role.Id, PermissionId = p2.Id });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.GetPermissionsAsync(role.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result!.Select(x => x.Code).Should().BeEquivalentTo("system.users.view", "system.users.create");
    }

    private static RoleService CreateService(
        AuthDbContext dbContext,
        Mock<ISearchIndexService>? searchIndexService = null)
    {
        searchIndexService ??= new Mock<ISearchIndexService>();
        return new RoleService(dbContext, searchIndexService.Object);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
