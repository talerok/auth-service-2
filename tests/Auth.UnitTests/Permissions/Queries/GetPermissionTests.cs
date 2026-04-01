using Auth.Application.Permissions.Queries.GetAllPermissions;
using Auth.Application.Permissions.Queries.GetPermissionById;
using Auth.Domain;
using Auth.Infrastructure.Permissions.Queries.GetAllPermissions;
using Auth.Infrastructure.Permissions.Queries.GetPermissionById;
using FluentAssertions;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Permissions.Queries;

public sealed class GetPermissionTests
{
    [Fact]
    public async Task GetAll_ReturnsAllPermissions()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Permissions.AddRange(
            new Permission { Domain = "test.domain", Bit = 0, Code = "perm.a", Description = "A" },
            new Permission { Domain = "test.domain", Bit = 1, Code = "perm.b", Description = "B" });
        await dbContext.SaveChangesAsync();
        var handler = new GetAllPermissionsQueryHandler(dbContext);

        var result = await handler.Handle(new GetAllPermissionsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(x => x.Code).Should().BeEquivalentTo("perm.a", "perm.b");
    }

    [Fact]
    public async Task GetAll_ExcludesSoftDeleted()
    {
        await using var dbContext = CreateDbContext();
        var deletedPerm = new Permission { Domain = "test.domain", Bit = 1, Code = "deleted", Description = "Deleted" };
        deletedPerm.SoftDelete();
        dbContext.Permissions.AddRange(
            new Permission { Domain = "test.domain", Bit = 0, Code = "active", Description = "Active" },
            deletedPerm);
        await dbContext.SaveChangesAsync();
        var handler = new GetAllPermissionsQueryHandler(dbContext);

        var result = await handler.Handle(new GetAllPermissionsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result.Single().Code.Should().Be("active");
    }

    [Fact]
    public async Task GetById_ExistingPermission_ReturnsDto()
    {
        await using var dbContext = CreateDbContext();
        var permission = new Permission { Domain = "test.domain", Bit = 3, Code = "perm.find", Description = "Findable", IsSystem = true };
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync();
        var handler = new GetPermissionByIdQueryHandler(dbContext);

        var result = await handler.Handle(new GetPermissionByIdQuery(permission.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(permission.Id);
        result.Domain.Should().Be("test.domain");
        result.Bit.Should().Be(3);
        result.Code.Should().Be("perm.find");
        result.Description.Should().Be("Findable");
        result.IsSystem.Should().BeTrue();
    }

    [Fact]
    public async Task GetById_NonExistentPermission_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetPermissionByIdQueryHandler(dbContext);

        var result = await handler.Handle(new GetPermissionByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }
}
