using Auth.Application;
using Auth.Application.Permissions.Commands.SoftDeletePermission;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Permissions.Commands.SoftDeletePermission;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Permissions.Commands;

public sealed class SoftDeletePermissionTests
{
    [Fact]
    public async Task SoftDelete_ExistingPermission_SoftDeletesAndReturnsTrue()
    {
        await using var dbContext = CreateDbContext();
        var permission = new Permission { Domain = "test.domain", Bit = 0, Code = "to.delete", Description = "desc" };
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var handler = new SoftDeletePermissionCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new SoftDeletePermissionCommand(permission.Id),
            CancellationToken.None);

        result.Should().BeTrue();
        var updated = await dbContext.Permissions.IgnoreQueryFilters().FirstAsync(x => x.Id == permission.Id);
        updated.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SoftDelete_NonExistentPermission_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();
        var eventBus = new Mock<IEventBus>();
        var handler = new SoftDeletePermissionCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

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
        var eventBus = new Mock<IEventBus>();
        var handler = new SoftDeletePermissionCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

        var act = () => handler.Handle(
            new SoftDeletePermissionCommand(permission.Id),
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<AuthException>();
        exception.Which.Code.Should().Be(AuthErrorCatalog.SystemPermissionDeleteForbidden);
    }
}
