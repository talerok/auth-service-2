using Auth.Application;
using Auth.Application.Permissions.Commands.CreatePermission;
using Auth.Application.Permissions.Commands.UpdatePermission;
using Auth.Application.Permissions.Commands.PatchPermission;
using Auth.Application.Permissions.Commands.SoftDeletePermission;
using Auth.Application.Permissions.Queries.GetAllPermissions;
using Auth.Application.Permissions.Queries.GetPermissionById;
using Auth.Application.Permissions.Queries.SearchPermissions;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Permissions.Commands.CreatePermission;
using Auth.Infrastructure.Permissions.Commands.UpdatePermission;
using Auth.Infrastructure.Permissions.Commands.PatchPermission;
using Auth.Infrastructure.Permissions.Commands.SoftDeletePermission;
using Auth.Infrastructure.Permissions.Queries.GetAllPermissions;
using Auth.Infrastructure.Permissions.Queries.GetPermissionById;
using Auth.Infrastructure.Permissions.Queries.SearchPermissions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Auth.UnitTests.Permissions;

public sealed class PermissionHandlerTests
{
    // ── CreatePermissionCommandHandler ──────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_CreatesPermissionAndIndexes()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        searchIndex.Setup(x => x.IndexPermissionAsync(It.IsAny<PermissionDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new CreatePermissionCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new CreatePermissionCommand("users.read", "Read users"),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Code.Should().Be("users.read");
        result.Description.Should().Be("Read users");
        result.IsSystem.Should().BeFalse();

        var entity = await dbContext.Permissions.FirstAsync(x => x.Id == result.Id);
        entity.Code.Should().Be("users.read");
        entity.Description.Should().Be("Read users");

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
            new CreatePermissionCommand("first.perm", "First"),
            CancellationToken.None);

        result.Bit.Should().Be(0);
    }

    [Fact]
    public async Task Create_ExistingPermissions_AssignsNextBit()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Permissions.Add(new Permission { Bit = 5, Code = "existing", Description = "Existing" });
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new CreatePermissionCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new CreatePermissionCommand("new.perm", "New permission"),
            CancellationToken.None);

        result.Bit.Should().Be(6);
    }

    [Fact]
    public async Task Create_DeletedPermissionWithHighBit_UsesDeletedBitForMax()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Permissions.Add(new Permission { Bit = 3, Code = "active", Description = "Active" });
        dbContext.Permissions.Add(new Permission { Bit = 7, Code = "deleted", Description = "Deleted", DeletedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new CreatePermissionCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new CreatePermissionCommand("after.deleted", "After deleted"),
            CancellationToken.None);

        result.Bit.Should().Be(8);
    }

    // ── UpdatePermissionCommandHandler ──────────────────────────────────

    [Fact]
    public async Task Update_ExistingPermission_UpdatesAndReturns()
    {
        await using var dbContext = CreateDbContext();
        var permission = new Permission { Bit = 0, Code = "perm.code", Description = "OldDesc" };
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        searchIndex.Setup(x => x.IndexPermissionAsync(It.IsAny<PermissionDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new UpdatePermissionCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new UpdatePermissionCommand(permission.Id, "NewDesc"),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Description.Should().Be("NewDesc");
        result.Code.Should().Be("perm.code");

        var entity = await dbContext.Permissions.FirstAsync(x => x.Id == permission.Id);
        entity.Description.Should().Be("NewDesc");

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
            new UpdatePermissionCommand(Guid.NewGuid(), "Desc"),
            CancellationToken.None);

        result.Should().BeNull();
    }

    // ── PatchPermissionCommandHandler ───────────────────────────────────

    [Fact]
    public async Task Patch_WithDescription_OnlyUpdatesDescription()
    {
        await using var dbContext = CreateDbContext();
        var permission = new Permission { Bit = 0, Code = "perm.code", Description = "OriginalDesc" };
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        searchIndex.Setup(x => x.IndexPermissionAsync(It.IsAny<PermissionDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new PatchPermissionCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new PatchPermissionCommand(permission.Id, "UpdatedDesc"),
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
        var permission = new Permission { Bit = 0, Code = "perm.code", Description = "OriginalDesc" };
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new PatchPermissionCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new PatchPermissionCommand(permission.Id, null),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Description.Should().Be("OriginalDesc");
    }

    [Fact]
    public async Task Patch_NonExistentPermission_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new PatchPermissionCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new PatchPermissionCommand(Guid.NewGuid(), "Desc"),
            CancellationToken.None);

        result.Should().BeNull();
    }

    // ── SoftDeletePermissionCommandHandler ──────────────────────────────

    [Fact]
    public async Task SoftDelete_ExistingPermission_SoftDeletesAndReturnsTrue()
    {
        await using var dbContext = CreateDbContext();
        var permission = new Permission { Bit = 0, Code = "to.delete", Description = "desc" };
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
        var permission = new Permission { Bit = 0, Code = "system.perm", Description = "System", IsSystem = true };
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

    // ── GetAllPermissionsQueryHandler ────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsAllPermissions()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Permissions.AddRange(
            new Permission { Bit = 0, Code = "perm.a", Description = "A" },
            new Permission { Bit = 1, Code = "perm.b", Description = "B" });
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
            new Permission { Bit = 0, Code = "active", Description = "Active" },
            new Permission { Bit = 1, Code = "deleted", Description = "Deleted", DeletedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();
        var handler = new GetAllPermissionsQueryHandler(dbContext);

        var result = await handler.Handle(new GetAllPermissionsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result.Single().Code.Should().Be("active");
    }

    // ── GetPermissionByIdQueryHandler ────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingPermission_ReturnsDto()
    {
        await using var dbContext = CreateDbContext();
        var permission = new Permission { Bit = 3, Code = "perm.find", Description = "Findable", IsSystem = true };
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync();
        var handler = new GetPermissionByIdQueryHandler(dbContext);

        var result = await handler.Handle(new GetPermissionByIdQuery(permission.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(permission.Id);
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

    // ── SearchPermissionsQueryHandler ────────────────────────────────────

    [Fact]
    public async Task Search_DelegatesToSearchService()
    {
        var searchService = new Mock<ISearchService>();
        var expectedItems = new List<PermissionDto>
        {
            new(Guid.NewGuid(), 0, "perm.a", "A", false),
            new(Guid.NewGuid(), 1, "perm.b", "B", true)
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

    // ── Helper ──────────────────────────────────────────────────────────

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
