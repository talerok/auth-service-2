using Auth.Application;
using Auth.Application.Permissions.Commands.CreatePermission;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Permissions.Commands.CreatePermission;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Permissions.Commands;

public sealed class CreatePermissionTests
{
    [Fact]
    public async Task Create_ValidRequest_CreatesPermissionAndIndexes()
    {
        await using var dbContext = CreateDbContext();
        var eventBus = new Mock<IEventBus>();
        var handler = new CreatePermissionCommandHandler(dbContext, eventBus.Object);

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
    }

    [Fact]
    public async Task Create_EmptyDatabase_AssignsBitZero()
    {
        await using var dbContext = CreateDbContext();
        var eventBus = new Mock<IEventBus>();
        var handler = new CreatePermissionCommandHandler(dbContext, eventBus.Object);

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
        var eventBus = new Mock<IEventBus>();
        var handler = new CreatePermissionCommandHandler(dbContext, eventBus.Object);

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
        var eventBus = new Mock<IEventBus>();
        var handler = new CreatePermissionCommandHandler(dbContext, eventBus.Object);

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
        var deletedPerm = new Permission { Domain = "other.domain", Bit = 7, Code = "deleted", Description = "Deleted" };
        deletedPerm.SoftDelete();
        dbContext.Permissions.Add(deletedPerm);
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var handler = new CreatePermissionCommandHandler(dbContext, eventBus.Object);

        var result = await handler.Handle(
            new CreatePermissionCommand("custom.domain", "after.deleted", "After deleted"),
            CancellationToken.None);

        result.Bit.Should().Be(0);
    }
}
