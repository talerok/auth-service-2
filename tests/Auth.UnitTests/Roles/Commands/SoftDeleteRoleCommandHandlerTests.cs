using Auth.Application;
using Auth.Application.Roles.Commands.SoftDeleteRole;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Roles.Commands.SoftDeleteRole;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Roles.Commands;

public sealed class SoftDeleteRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExistingRole_SoftDeletesAndReturnsTrue()
    {
        await using var dbContext = CreateDbContext();
        var role = new Role { Name = "ToDelete", Description = "desc" };
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var handler = new SoftDeleteRoleCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new SoftDeleteRoleCommand(role.Id),
            CancellationToken.None);

        result.Should().BeTrue();
        var updated = await dbContext.Roles.IgnoreQueryFilters().FirstAsync(x => x.Id == role.Id);
        updated.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_NonExistentRole_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();
        var eventBus = new Mock<IEventBus>();
        var handler = new SoftDeleteRoleCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new SoftDeleteRoleCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.Should().BeFalse();
    }

}
