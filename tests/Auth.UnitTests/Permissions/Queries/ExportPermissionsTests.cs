using Auth.Application.Permissions.Queries.ExportPermissions;
using Auth.Domain;
using Auth.Infrastructure.Permissions.Queries.ExportPermissions;
using FluentAssertions;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Permissions.Queries;

public sealed class ExportPermissionsTests
{
    [Fact]
    public async Task Export_ReturnsOnlyCustomPermissions()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Permissions.AddRange(
            new Permission { Domain = "test.domain", Bit = 0, Code = "system.perm", Description = "System", IsSystem = true },
            new Permission { Domain = "test.domain", Bit = 1, Code = "custom.a", Description = "Custom A" },
            new Permission { Domain = "test.domain", Bit = 2, Code = "custom.b", Description = "Custom B" });
        await dbContext.SaveChangesAsync();
        var handler = new ExportPermissionsQueryHandler(dbContext);

        var result = await handler.Handle(new ExportPermissionsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(x => x.Code).Should().BeEquivalentTo("custom.a", "custom.b");
        result.Should().AllSatisfy(x => x.Domain.Should().Be("test.domain"));
    }

    [Fact]
    public async Task Export_ExcludesSoftDeleted()
    {
        await using var dbContext = CreateDbContext();
        var deletedPerm = new Permission { Domain = "test.domain", Bit = 1, Code = "deleted", Description = "Deleted" };
        deletedPerm.SoftDelete();
        dbContext.Permissions.AddRange(
            new Permission { Domain = "test.domain", Bit = 0, Code = "active", Description = "Active" },
            deletedPerm);
        await dbContext.SaveChangesAsync();
        var handler = new ExportPermissionsQueryHandler(dbContext);

        var result = await handler.Handle(new ExportPermissionsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result.Single().Code.Should().Be("active");
    }

    [Fact]
    public async Task Export_OrdersByBit()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Permissions.AddRange(
            new Permission { Domain = "test.domain", Bit = 5, Code = "later", Description = "Later" },
            new Permission { Domain = "test.domain", Bit = 0, Code = "first", Description = "First" });
        await dbContext.SaveChangesAsync();
        var handler = new ExportPermissionsQueryHandler(dbContext);

        var result = await handler.Handle(new ExportPermissionsQuery(), CancellationToken.None);

        result.Select(x => x.Bit).Should().BeInAscendingOrder();
    }
}
