using Auth.Application;
using Auth.Application.Permissions.Commands.CreatePermission;
using Auth.Application.Permissions.Commands.ImportPermissions;
using Auth.Application.Permissions.Commands.UpdatePermission;
using Auth.Application.Permissions.Commands.PatchPermission;
using Auth.Application.Permissions.Commands.SoftDeletePermission;
using Auth.Application.Permissions.Queries.ExportPermissions;
using Auth.Application.Permissions.Queries.GetAllPermissions;
using Auth.Application.Permissions.Queries.GetPermissionById;
using Auth.Application.Permissions.Queries.SearchPermissions;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Permissions.Commands.CreatePermission;
using Auth.Infrastructure.Permissions.Commands.ImportPermissions;
using Auth.Infrastructure.Permissions.Commands.UpdatePermission;
using Auth.Infrastructure.Permissions.Commands.PatchPermission;
using Auth.Infrastructure.Permissions.Commands.SoftDeletePermission;
using Auth.Infrastructure.Permissions.Queries.ExportPermissions;
using Auth.Infrastructure.Permissions.Queries.GetAllPermissions;
using Auth.Infrastructure.Permissions.Queries.GetPermissionById;
using Auth.Infrastructure.Permissions.Queries.SearchPermissions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Auth.UnitTests.Permissions;

public sealed class PermissionHandlerTests
{
    // -- CreatePermissionCommandHandler --────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_CreatesPermissionAndIndexes()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        searchIndex.Setup(x => x.IndexPermissionAsync(It.IsAny<PermissionDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new CreatePermissionCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new CreatePermissionCommand("custom.domain", "users.read", "Read users"),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Code.Should().Be("users.read");
        result.Description.Should().Be("Read users");
        result.IsSystem.Should().BeFalse();
        result.Domain.Should().Be("custom.domain");

        var entity = await dbContext.Permissions.FirstAsync(x => x.Id == result.Id);
        entity.Code.Should().Be("users.read");
        entity.Description.Should().Be("Read users");
        entity.Domain.Should().Be("custom.domain");

        searchIndex.Verify(
            x => x.IndexPermissionAsync(It.Is<PermissionDto>(p => p.Id == result.Id), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Create_EmptyDatabase_AssignsBitZero()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new CreatePermissionCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new CreatePermissionCommand("custom.domain", "first.perm", "First"),
            CancellationToken.None);

        result.Bit.Should().Be(0);
    }

    [Fact]
    public async Task Create_OnlySystemPermissionsInDifferentDomain_AssignsBitZero()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Permissions.Add(new Permission { Domain = "system.domain", Bit = 5, Code = "existing", Description = "Existing", IsSystem = true });
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new CreatePermissionCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new CreatePermissionCommand("custom.domain", "new.perm", "New permission"),
            CancellationToken.None);

        result.Bit.Should().Be(0);
    }

    [Fact]
    public async Task Create_ExistingCustomPermission_AssignsNextBit()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Permissions.Add(new Permission { Domain = "custom.domain", Bit = 3, Code = "custom.existing", Description = "Existing custom" });
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new CreatePermissionCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new CreatePermissionCommand("custom.domain", "new.perm", "New permission"),
            CancellationToken.None);

        result.Bit.Should().Be(4);
    }

    [Fact]
    public async Task Create_DeletedPermissionInDomain_AssignsBitZero()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Permissions.Add(new Permission { Domain = "system.domain", Bit = 3, Code = "active", Description = "Active", IsSystem = true });
        dbContext.Permissions.Add(new Permission { Domain = "other.domain", Bit = 7, Code = "deleted", Description = "Deleted", DeletedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new CreatePermissionCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new CreatePermissionCommand("custom.domain", "after.deleted", "After deleted"),
            CancellationToken.None);

        result.Bit.Should().Be(0);
    }

    // -- UpdatePermissionCommandHandler --────────────────────────────────

    [Fact]
    public async Task Update_ExistingPermission_UpdatesAndReturns()
    {
        await using var dbContext = CreateDbContext();
        var permission = new Permission { Domain = "test.domain", Bit = 0, Code = "perm.code", Description = "OldDesc" };
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        searchIndex.Setup(x => x.IndexPermissionAsync(It.IsAny<PermissionDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new UpdatePermissionCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new UpdatePermissionCommand(permission.Id, "new.code", "NewDesc"),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Description.Should().Be("NewDesc");
        result.Code.Should().Be("new.code");

        var entity = await dbContext.Permissions.FirstAsync(x => x.Id == permission.Id);
        entity.Description.Should().Be("NewDesc");
        entity.Code.Should().Be("new.code");

        searchIndex.Verify(
            x => x.IndexPermissionAsync(It.Is<PermissionDto>(p => p.Id == permission.Id), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Update_NonExistentPermission_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new UpdatePermissionCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new UpdatePermissionCommand(Guid.NewGuid(), "code", "Desc"),
            CancellationToken.None);

        result.Should().BeNull();
    }

    // -- PatchPermissionCommandHandler --─────────────────────────────────

    [Fact]
    public async Task Patch_WithDescription_OnlyUpdatesDescription()
    {
        await using var dbContext = CreateDbContext();
        var permission = new Permission { Domain = "test.domain", Bit = 0, Code = "perm.code", Description = "OriginalDesc" };
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        searchIndex.Setup(x => x.IndexPermissionAsync(It.IsAny<PermissionDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new PatchPermissionCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new PatchPermissionCommand(permission.Id, null, "UpdatedDesc"),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Description.Should().Be("UpdatedDesc");
        result.Code.Should().Be("perm.code");

        var entity = await dbContext.Permissions.FirstAsync(x => x.Id == permission.Id);
        entity.Description.Should().Be("UpdatedDesc");

        searchIndex.Verify(
            x => x.IndexPermissionAsync(It.Is<PermissionDto>(p => p.Id == permission.Id), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Patch_WithNullDescription_PreservesOriginalDescription()
    {
        await using var dbContext = CreateDbContext();
        var permission = new Permission { Domain = "test.domain", Bit = 0, Code = "perm.code", Description = "OriginalDesc" };
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new PatchPermissionCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new PatchPermissionCommand(permission.Id, null, null),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Description.Should().Be("OriginalDesc");
        result.Code.Should().Be("perm.code");
    }

    [Fact]
    public async Task Patch_NonExistentPermission_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new PatchPermissionCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new PatchPermissionCommand(Guid.NewGuid(), null, "Desc"),
            CancellationToken.None);

        result.Should().BeNull();
    }

    // -- SoftDeletePermissionCommandHandler --────────────────────────────

    [Fact]
    public async Task SoftDelete_ExistingPermission_SoftDeletesAndReturnsTrue()
    {
        await using var dbContext = CreateDbContext();
        var permission = new Permission { Domain = "test.domain", Bit = 0, Code = "to.delete", Description = "desc" };
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new SoftDeletePermissionCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new SoftDeletePermissionCommand(permission.Id),
            CancellationToken.None);

        result.Should().BeTrue();
        var updated = await dbContext.Permissions.IgnoreQueryFilters().FirstAsync(x => x.Id == permission.Id);
        updated.DeletedAt.Should().NotBeNull();
        searchIndex.Verify(x => x.DeletePermissionAsync(permission.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SoftDelete_NonExistentPermission_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new SoftDeletePermissionCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new SoftDeletePermissionCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SoftDelete_SystemPermission_ThrowsAuthException()
    {
        await using var dbContext = CreateDbContext();
        var permission = new Permission { Domain = "test.domain", Bit = 0, Code = "system.perm", Description = "System", IsSystem = true };
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new SoftDeletePermissionCommandHandler(dbContext, searchIndex.Object);

        var act = () => handler.Handle(
            new SoftDeletePermissionCommand(permission.Id),
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<AuthException>();
        exception.Which.Code.Should().Be(AuthErrorCatalog.SystemPermissionDeleteForbidden);
    }

    // -- GetAllPermissionsQueryHandler --──────────────────────────────────

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
        dbContext.Permissions.AddRange(
            new Permission { Domain = "test.domain", Bit = 0, Code = "active", Description = "Active" },
            new Permission { Domain = "test.domain", Bit = 1, Code = "deleted", Description = "Deleted", DeletedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();
        var handler = new GetAllPermissionsQueryHandler(dbContext);

        var result = await handler.Handle(new GetAllPermissionsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result.Single().Code.Should().Be("active");
    }

    // -- GetPermissionByIdQueryHandler --──────────────────────────────────

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

    // -- SearchPermissionsQueryHandler --──────────────────────────────────

    [Fact]
    public async Task Search_DelegatesToSearchService()
    {
        var searchService = new Mock<ISearchService>();
        var expectedItems = new List<PermissionDto>
        {
            new(Guid.NewGuid(), "test.domain", 0, "perm.a", "A", false),
            new(Guid.NewGuid(), "test.domain", 1, "perm.b", "B", true)
        };
        var expectedResponse = new SearchResponse<PermissionDto>(expectedItems, 1, 20, 2);
        var request = new SearchRequest(null, null);
        searchService
            .Setup(x => x.SearchPermissionsAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);
        var handler = new SearchPermissionsQueryHandler(searchService.Object);

        var result = await handler.Handle(
            new SearchPermissionsQuery(request),
            CancellationToken.None);

        result.Should().BeSameAs(expectedResponse);
        searchService.Verify(
            x => x.SearchPermissionsAsync(request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -- ExportPermissionsQueryHandler --─────────────────────────────────

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
        dbContext.Permissions.AddRange(
            new Permission { Domain = "test.domain", Bit = 0, Code = "active", Description = "Active" },
            new Permission { Domain = "test.domain", Bit = 1, Code = "deleted", Description = "Deleted", DeletedAt = DateTime.UtcNow });
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

    // -- ImportPermissionsCommandHandler --────────────────────────────────

    [Fact]
    public async Task Import_CreatesNewPermissions()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new ImportPermissionsCommandHandler(dbContext, searchIndex.Object);

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
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new ImportPermissionsCommandHandler(dbContext, searchIndex.Object);

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
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new ImportPermissionsCommandHandler(dbContext, searchIndex.Object);

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
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new ImportPermissionsCommandHandler(dbContext, searchIndex.Object);

        var items = new List<ImportPermissionItem> { new("custom.domain", 5, "system.hack", "Hack") };
        var act = () => handler.Handle(new ImportPermissionsCommand(items), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<AuthException>();
        exception.Which.Code.Should().Be(AuthErrorCatalog.SystemPermissionImportForbidden);
    }

    [Fact]
    public async Task Import_RestoresSoftDeletedPermission()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Permissions.Add(new Permission { Domain = "custom.domain", Bit = 0, Code = "deleted", Description = "Deleted", DeletedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new ImportPermissionsCommandHandler(dbContext, searchIndex.Object);

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
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new ImportPermissionsCommandHandler(dbContext, searchIndex.Object);

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
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new ImportPermissionsCommandHandler(dbContext, searchIndex.Object);

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
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new ImportPermissionsCommandHandler(dbContext, searchIndex.Object);

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

    // -- Helper --────────────────────────────────────────────────────────

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
