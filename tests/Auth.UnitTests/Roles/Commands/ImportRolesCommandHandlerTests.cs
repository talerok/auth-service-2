using Auth.Application;
using Auth.Application.Roles.Commands.ImportRoles;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Roles.Commands.ImportRoles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Auth.UnitTests.Roles.Commands;

public sealed class ImportRolesCommandHandlerTests
{
    [Fact]
    public async Task Import_CreatesNewRoles()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Permissions.Add(new Permission { Bit = 128, Code = "perm.a", Description = "A" });
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new ImportRolesCommandHandler(dbContext, searchIndex.Object);

        var items = new List<ImportRoleItem> { new("Editor", "editor", "Can edit", ["perm.a"]) };
        var result = await handler.Handle(new ImportRolesCommand(items), CancellationToken.None);

        result.Created.Should().Be(1);
        result.Updated.Should().Be(0);
        var role = await dbContext.Roles.FirstAsync(r => r.Name == "Editor");
        role.Code.Should().Be("editor");
        role.Description.Should().Be("Can edit");
        var rp = await dbContext.RolePermissions.Where(x => x.RoleId == role.Id).ToListAsync();
        rp.Should().HaveCount(1);
    }

    [Fact]
    public async Task Import_UpdatesExistingRoles()
    {
        await using var dbContext = CreateDbContext();
        var perm = new Permission { Bit = 128, Code = "perm.a", Description = "A" };
        dbContext.Permissions.Add(perm);
        var role = new Role { Name = "Editor", Code = "editor", Description = "Old" };
        dbContext.Roles.Add(role);
        dbContext.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = perm.Id });
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new ImportRolesCommandHandler(dbContext, searchIndex.Object);

        var items = new List<ImportRoleItem> { new("Editor", "editor", "New desc", ["perm.a"]) };
        var result = await handler.Handle(new ImportRolesCommand(items), CancellationToken.None);

        result.Created.Should().Be(0);
        result.Updated.Should().Be(1);
        var updated = await dbContext.Roles.FirstAsync(r => r.Name == "Editor");
        updated.Description.Should().Be("New desc");
    }

    [Fact]
    public async Task Import_MissingPermissionCode_ThrowsAuthException()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new ImportRolesCommandHandler(dbContext, searchIndex.Object);

        var items = new List<ImportRoleItem> { new("Role", "role", "Desc", ["nonexistent.perm"]) };
        var act = () => handler.Handle(new ImportRolesCommand(items), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<AuthException>();
        exception.Which.Code.Should().Be(AuthErrorCatalog.PermissionCodeNotFound);
    }

    [Fact]
    public async Task Import_MixedNewAndExisting_ReturnsCorrectCounts()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Permissions.Add(new Permission { Bit = 128, Code = "perm.a", Description = "A" });
        dbContext.Roles.Add(new Role { Name = "Existing", Code = "existing", Description = "Old" });
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new ImportRolesCommandHandler(dbContext, searchIndex.Object);

        var items = new List<ImportRoleItem>
        {
            new("Existing", "existing", "Updated", ["perm.a"]),
            new("NewRole", "new-role", "Brand new", ["perm.a"])
        };
        var result = await handler.Handle(new ImportRolesCommand(items), CancellationToken.None);

        result.Created.Should().Be(1);
        result.Updated.Should().Be(1);
    }

    [Fact]
    public async Task Import_ReplacesPermissions()
    {
        await using var dbContext = CreateDbContext();
        var permA = new Permission { Bit = 128, Code = "perm.a", Description = "A" };
        var permB = new Permission { Bit = 129, Code = "perm.b", Description = "B" };
        dbContext.Permissions.AddRange(permA, permB);
        var role = new Role { Name = "Editor", Code = "editor", Description = "Desc" };
        dbContext.Roles.Add(role);
        dbContext.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permA.Id });
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new ImportRolesCommandHandler(dbContext, searchIndex.Object);

        var items = new List<ImportRoleItem> { new("Editor", "editor", "Desc", ["perm.b"]) };
        await handler.Handle(new ImportRolesCommand(items), CancellationToken.None);

        var rps = await dbContext.RolePermissions.Where(x => x.RoleId == role.Id).Include(x => x.Permission).ToListAsync();
        rps.Should().HaveCount(1);
        rps.Single().Permission!.Code.Should().Be("perm.b");
    }

    [Fact]
    public async Task Import_RestoresSoftDeletedRole()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Permissions.Add(new Permission { Bit = 128, Code = "perm.a", Description = "A" });
        dbContext.Roles.Add(new Role { Name = "Deleted", Code = "deleted", Description = "Old", DeletedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new ImportRolesCommandHandler(dbContext, searchIndex.Object);

        var items = new List<ImportRoleItem> { new("Deleted", "deleted", "Restored", ["perm.a"]) };
        var result = await handler.Handle(new ImportRolesCommand(items), CancellationToken.None);

        result.Updated.Should().Be(1);
        var role = await dbContext.Roles.IgnoreQueryFilters().FirstAsync(r => r.Name == "Deleted");
        role.DeletedAt.Should().BeNull();
        role.Description.Should().Be("Restored");
    }

    [Fact]
    public async Task Import_AddFalse_SkipsNewRoles()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Permissions.Add(new Permission { Bit = 128, Code = "perm.a", Description = "A" });
        dbContext.Roles.Add(new Role { Name = "Existing", Code = "existing", Description = "Old" });
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new ImportRolesCommandHandler(dbContext, searchIndex.Object);

        var items = new List<ImportRoleItem>
        {
            new("Existing", "existing", "Updated", ["perm.a"]),
            new("NewRole", "new-role", "Brand new", ["perm.a"])
        };
        var result = await handler.Handle(new ImportRolesCommand(items, Add: false), CancellationToken.None);

        result.Created.Should().Be(0);
        result.Updated.Should().Be(1);
        result.Skipped.Should().Be(1);
        (await dbContext.Roles.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Import_EditFalse_SkipsExistingRoles()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Permissions.Add(new Permission { Bit = 128, Code = "perm.a", Description = "A" });
        dbContext.Roles.Add(new Role { Name = "Existing", Code = "existing", Description = "Old" });
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new ImportRolesCommandHandler(dbContext, searchIndex.Object);

        var items = new List<ImportRoleItem>
        {
            new("Existing", "existing", "Updated", ["perm.a"]),
            new("NewRole", "new-role", "Brand new", ["perm.a"])
        };
        var result = await handler.Handle(new ImportRolesCommand(items, Edit: false), CancellationToken.None);

        result.Created.Should().Be(1);
        result.Updated.Should().Be(0);
        result.Skipped.Should().Be(1);
        var existing = await dbContext.Roles.FirstAsync(r => r.Name == "Existing");
        existing.Description.Should().Be("Old");
    }

    [Fact]
    public async Task Import_BothFalse_SkipsAll()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Permissions.Add(new Permission { Bit = 128, Code = "perm.a", Description = "A" });
        dbContext.Roles.Add(new Role { Name = "Existing", Code = "existing", Description = "Old" });
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new ImportRolesCommandHandler(dbContext, searchIndex.Object);

        var items = new List<ImportRoleItem>
        {
            new("Existing", "existing", "Updated", ["perm.a"]),
            new("NewRole", "new-role", "Brand new", ["perm.a"])
        };
        var result = await handler.Handle(new ImportRolesCommand(items, Add: false, Edit: false), CancellationToken.None);

        result.Created.Should().Be(0);
        result.Updated.Should().Be(0);
        result.Skipped.Should().Be(2);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
