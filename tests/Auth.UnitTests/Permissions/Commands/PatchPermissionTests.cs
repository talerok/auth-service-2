using Auth.Application;
using Auth.Application.Permissions.Commands.PatchPermission;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Permissions.Commands.PatchPermission;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Permissions.Commands;

public sealed class PatchPermissionTests
{
    [Fact]
    public async Task Patch_WithDescription_OnlyUpdatesDescription()
    {
        await using var dbContext = CreateDbContext();
        var permission = new Permission { Domain = "test.domain", Bit = 0, Code = "perm.code", Description = "OriginalDesc" };
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var handler = new PatchPermissionCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new PatchPermissionCommand(permission.Id, default, "UpdatedDesc"),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Description.Should().Be("UpdatedDesc");
        result.Code.Should().Be("perm.code");

        var entity = await dbContext.Permissions.FirstAsync(x => x.Id == permission.Id);
        entity.Description.Should().Be("UpdatedDesc");
    }

    [Fact]
    public async Task Patch_WithNullDescription_PreservesOriginalDescription()
    {
        await using var dbContext = CreateDbContext();
        var permission = new Permission { Domain = "test.domain", Bit = 0, Code = "perm.code", Description = "OriginalDesc" };
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var handler = new PatchPermissionCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new PatchPermissionCommand(permission.Id, default, default),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Description.Should().Be("OriginalDesc");
        result.Code.Should().Be("perm.code");
    }

    [Fact]
    public async Task Patch_NonExistentPermission_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var eventBus = new Mock<IEventBus>();
        var handler = new PatchPermissionCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new PatchPermissionCommand(Guid.NewGuid(), default, "Desc"),
            CancellationToken.None);

        result.Should().BeNull();
    }
}
