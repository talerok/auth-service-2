using Auth.Application;
using Auth.Application.Roles.Queries.ExportRoles;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Roles.Queries.ExportRoles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Auth.UnitTests.Roles.Queries;

public sealed class ExportRolesQueryHandlerTests
{
    [Fact]
    public async Task Export_ReturnsRolesWithPermissionCodes()
    {
        await using var dbContext = CreateDbContext();
        var perm = new Permission { Bit = 128, Code = "perm.a", Description = "A" };
        dbContext.Permissions.Add(perm);
        var role = new Role { Name = "Editor", Description = "Can edit" };
        dbContext.Roles.Add(role);
        dbContext.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = perm.Id });
        await dbContext.SaveChangesAsync();
        var handler = new ExportRolesQueryHandler(dbContext);

        var result = await handler.Handle(new ExportRolesQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        var exported = result.Single();
        exported.Name.Should().Be("Editor");
        exported.Description.Should().Be("Can edit");
        exported.Permissions.Should().ContainSingle("perm.a");
    }

    [Fact]
    public async Task Export_ExcludesSoftDeleted()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Roles.AddRange(
            new Role { Name = "Active", Description = "Active" },
            new Role { Name = "Deleted", Description = "Deleted", DeletedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();
        var handler = new ExportRolesQueryHandler(dbContext);

        var result = await handler.Handle(new ExportRolesQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result.Single().Name.Should().Be("Active");
    }

    [Fact]
    public async Task Export_OrdersByName()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Roles.AddRange(
            new Role { Name = "Zebra", Description = "Z" },
            new Role { Name = "Alpha", Description = "A" });
        await dbContext.SaveChangesAsync();
        var handler = new ExportRolesQueryHandler(dbContext);

        var result = await handler.Handle(new ExportRolesQuery(), CancellationToken.None);

        result.Select(x => x.Name).Should().BeInAscendingOrder();
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
