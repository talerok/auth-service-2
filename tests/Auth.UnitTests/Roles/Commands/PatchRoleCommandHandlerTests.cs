using Auth.Application;
using Auth.Application.Roles.Commands.PatchRole;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Roles.Commands.PatchRole;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Roles.Commands;

public sealed class PatchRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_PatchName_OnlyUpdatesName()
    {
        await using var dbContext = CreateDbContext();
        var role = new Role { Name = "Original", Code = "original", Description = "OriginalDesc" };
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();
        var eventBus = new Mock<IEventBus>();
        var handler = new PatchRoleCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new PatchRoleCommand(role.Id, "Updated", default, default),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated");
        result.Code.Should().Be("original");
        result.Description.Should().Be("OriginalDesc");

        var entity = await dbContext.Roles.FirstAsync(x => x.Id == role.Id);
        entity.Name.Should().Be("Updated");
        entity.Code.Should().Be("original");
        entity.Description.Should().Be("OriginalDesc");
    }

    [Fact]
    public async Task Handle_NonExistentRole_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var eventBus = new Mock<IEventBus>();
        var handler = new PatchRoleCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new PatchRoleCommand(Guid.NewGuid(), "Name", default, default),
            CancellationToken.None);

        result.Should().BeNull();
    }

}
