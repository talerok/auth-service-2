using Auth.Application;
using Auth.Application.Permissions.Commands.ImportPermissions;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Permissions.Commands.ImportPermissions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Permissions.Commands;

public sealed class ImportPermissionsTests
{
    [Fact]
    public async Task Import_CreatesNewPermissions()
    {
        await using var dbContext = CreateDbContext();
        var eventBus = new Mock<IEventBus>();
        var handler = new ImportPermissionsCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

        var items = new List<ImportPermissionItem>
        {
            new("custom.domain", 0, "perm.a", "A"),
            new("custom.domain", 1, "perm.b", "B")
        };
        var result = await handler.Handle(new ImportPermissionsCommand(items), CancellationToken.None);

        result.Created.Should().Be(2);
        result.Updated.Should().Be(0);
        var all = await dbContext.Permissions.ToListAsync();
        all.Should().HaveCount(2);
        all.Should().AllSatisfy(x => x.IsSystem.Should().BeFalse());
        all.Should().AllSatisfy(x => x.Domain.Should().Be("custom.domain"));
    }

    [Fact]
    public async Task Import_UpdatesExistingPermissions()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Permissions.Add(new Permission { Domain = "custom.domain", Bit = 0, Code = "old.code", Description = "Old" });
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var handler = new ImportPermissionsCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

        var items = new List<ImportPermissionItem> { new("custom.domain", 0, "new.code", "New") };
        var result = await handler.Handle(new ImportPermissionsCommand(items), CancellationToken.None);

        result.Created.Should().Be(0);
        result.Updated.Should().Be(1);
        var entity = await dbContext.Permissions.FirstAsync(x => x.Domain == "custom.domain" && x.Bit == 0);
        entity.Code.Should().Be("new.code");
        entity.Description.Should().Be("New");
    }

    [Fact]
    public async Task Import_MixedNewAndExisting_ReturnsCorrectCounts()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Permissions.Add(new Permission { Domain = "custom.domain", Bit = 0, Code = "existing", Description = "Existing" });
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var handler = new ImportPermissionsCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

        var items = new List<ImportPermissionItem>
        {
            new("custom.domain", 0, "updated", "Updated"),
            new("custom.domain", 1, "created", "Created")
        };
        var result = await handler.Handle(new ImportPermissionsCommand(items), CancellationToken.None);

        result.Created.Should().Be(1);
        result.Updated.Should().Be(1);
    }

    [Fact]
    public async Task Import_SystemPermissionAtDomainBit_ThrowsAuthException()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Permissions.Add(new Permission { Domain = "custom.domain", Bit = 5, Code = "system.perm", Description = "System", IsSystem = true });
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var handler = new ImportPermissionsCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

        var items = new List<ImportPermissionItem> { new("custom.domain", 5, "system.hack", "Hack") };
        var act = () => handler.Handle(new ImportPermissionsCommand(items), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<AuthException>();
        exception.Which.Code.Should().Be(AuthErrorCatalog.SystemPermissionImportForbidden);
    }

    [Fact]
    public async Task Import_RestoresSoftDeletedPermission()
    {
        await using var dbContext = CreateDbContext();
        var deletedPerm = new Permission { Domain = "custom.domain", Bit = 0, Code = "deleted", Description = "Deleted" };
        deletedPerm.SoftDelete();
        dbContext.Permissions.Add(deletedPerm);
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var handler = new ImportPermissionsCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

        var items = new List<ImportPermissionItem> { new("custom.domain", 0, "restored", "Restored") };
        var result = await handler.Handle(new ImportPermissionsCommand(items), CancellationToken.None);

        result.Updated.Should().Be(1);
        var entity = await dbContext.Permissions.IgnoreQueryFilters().FirstAsync(x => x.Domain == "custom.domain" && x.Bit == 0);
        entity.DeletedAt.Should().BeNull();
        entity.Code.Should().Be("restored");
    }

    [Fact]
    public async Task Import_AddFalse_SkipsNewPermissions()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Permissions.Add(new Permission { Domain = "custom.domain", Bit = 0, Code = "existing", Description = "Existing" });
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var handler = new ImportPermissionsCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

        var items = new List<ImportPermissionItem>
        {
            new("custom.domain", 0, "updated", "Updated"),
            new("custom.domain", 1, "new.perm", "New")
        };
        var result = await handler.Handle(new ImportPermissionsCommand(items, Add: false), CancellationToken.None);

        result.Created.Should().Be(0);
        result.Updated.Should().Be(1);
        result.Skipped.Should().Be(1);
        (await dbContext.Permissions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Import_EditFalse_SkipsExistingPermissions()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Permissions.Add(new Permission { Domain = "custom.domain", Bit = 0, Code = "existing", Description = "Existing" });
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var handler = new ImportPermissionsCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

        var items = new List<ImportPermissionItem>
        {
            new("custom.domain", 0, "updated", "Updated"),
            new("custom.domain", 1, "new.perm", "New")
        };
        var result = await handler.Handle(new ImportPermissionsCommand(items, Edit: false), CancellationToken.None);

        result.Created.Should().Be(1);
        result.Updated.Should().Be(0);
        result.Skipped.Should().Be(1);
        var entity = await dbContext.Permissions.FirstAsync(x => x.Domain == "custom.domain" && x.Bit == 0);
        entity.Code.Should().Be("existing");
    }

    [Fact]
    public async Task Import_BothFalse_SkipsAll()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Permissions.Add(new Permission { Domain = "custom.domain", Bit = 0, Code = "existing", Description = "Existing" });
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var handler = new ImportPermissionsCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

        var items = new List<ImportPermissionItem>
        {
            new("custom.domain", 0, "updated", "Updated"),
            new("custom.domain", 1, "new.perm", "New")
        };
        var result = await handler.Handle(new ImportPermissionsCommand(items, Add: false, Edit: false), CancellationToken.None);

        result.Created.Should().Be(0);
        result.Updated.Should().Be(0);
        result.Skipped.Should().Be(2);
    }
}
