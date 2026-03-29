using Auth.Application;
using Auth.Application.Roles.Commands.UpdateRole;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Roles.Commands.UpdateRole;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Roles.Commands;

public sealed class UpdateRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExistingRole_UpdatesAndReturns()
    {
        await using var dbContext = CreateDbContext();
        var role = new Role { Name = "OldName", Code = "old-code", Description = "OldDesc" };
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var handler = new UpdateRoleCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new UpdateRoleCommand(role.Id, "NewName", "new-code", "NewDesc"),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("NewName");
        result.Code.Should().Be("new-code");
        result.Description.Should().Be("NewDesc");

        var entity = await dbContext.Roles.FirstAsync(x => x.Id == role.Id);
        entity.Name.Should().Be("NewName");
        entity.Code.Should().Be("new-code");
        entity.Description.Should().Be("NewDesc");
    }

    [Fact]
    public async Task Handle_NonExistentRole_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var eventBus = new Mock<IEventBus>();
        var handler = new UpdateRoleCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new UpdateRoleCommand(Guid.NewGuid(), "Name", "code", "Desc"),
            CancellationToken.None);

        result.Should().BeNull();
    }

}
