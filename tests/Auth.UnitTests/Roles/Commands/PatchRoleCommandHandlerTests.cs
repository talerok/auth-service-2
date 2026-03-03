using Auth.Application;
using Auth.Application.Roles.Commands.PatchRole;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Roles.Commands.PatchRole;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Auth.UnitTests.Roles.Commands;

public sealed class PatchRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_PatchName_OnlyUpdatesName()
    {
        await using var dbContext = CreateDbContext();
        var role = new Role { Name = "Original", Description = "OriginalDesc" };
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        searchIndex.Setup(x => x.IndexRoleAsync(It.IsAny<RoleDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new PatchRoleCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new PatchRoleCommand(role.Id, "Updated", null),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated");
        result.Description.Should().Be("OriginalDesc");

        var entity = await dbContext.Roles.FirstAsync(x => x.Id == role.Id);
        entity.Name.Should().Be("Updated");
        entity.Description.Should().Be("OriginalDesc");

        searchIndex.Verify(x => x.IndexRoleAsync(It.Is<RoleDto>(r => r.Id == role.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentRole_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new PatchRoleCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new PatchRoleCommand(Guid.NewGuid(), "Name", null),
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
