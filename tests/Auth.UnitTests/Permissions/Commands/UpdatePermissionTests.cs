using Auth.Application;
using Auth.Application.Permissions.Commands.UpdatePermission;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Permissions.Commands.UpdatePermission;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Permissions.Commands;

public sealed class UpdatePermissionTests
{
    [Fact]
    public async Task Update_ExistingPermission_UpdatesAndReturns()
    {
        await using var dbContext = CreateDbContext();
        var permission = new Permission { Domain = "test.domain", Bit = 0, Code = "perm.code", Description = "OldDesc" };
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var handler = new UpdatePermissionCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new UpdatePermissionCommand(permission.Id, "new.code", "NewDesc"),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Description.Should().Be("NewDesc");
        result.Code.Should().Be("new.code");

        var entity = await dbContext.Permissions.FirstAsync(x => x.Id == permission.Id);
        entity.Description.Should().Be("NewDesc");
        entity.Code.Should().Be("new.code");
    }

    [Fact]
    public async Task Update_NonExistentPermission_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var eventBus = new Mock<IEventBus>();
        var handler = new UpdatePermissionCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new UpdatePermissionCommand(Guid.NewGuid(), "code", "Desc"),
            CancellationToken.None);

        result.Should().BeNull();
    }
}
