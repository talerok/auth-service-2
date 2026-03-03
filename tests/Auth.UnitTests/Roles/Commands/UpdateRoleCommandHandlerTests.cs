using Auth.Application;
using Auth.Application.Roles.Commands.UpdateRole;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Roles.Commands.UpdateRole;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Auth.UnitTests.Roles.Commands;

public sealed class UpdateRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExistingRole_UpdatesAndReturns()
    {
        await using var dbContext = CreateDbContext();
        var role = new Role { Name = "OldName", Description = "OldDesc" };
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        searchIndex.Setup(x => x.IndexRoleAsync(It.IsAny<RoleDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new UpdateRoleCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new UpdateRoleCommand(role.Id, "NewName", "NewDesc"),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("NewName");
        result.Description.Should().Be("NewDesc");

        var entity = await dbContext.Roles.FirstAsync(x => x.Id == role.Id);
        entity.Name.Should().Be("NewName");
        entity.Description.Should().Be("NewDesc");

        searchIndex.Verify(x => x.IndexRoleAsync(It.Is<RoleDto>(r => r.Id == role.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentRole_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new UpdateRoleCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new UpdateRoleCommand(Guid.NewGuid(), "Name", "Desc"),
            CancellationToken.None);

        result.Should().BeNull();
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
