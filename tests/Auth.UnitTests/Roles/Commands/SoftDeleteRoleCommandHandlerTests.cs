using Auth.Application;
using Auth.Application.Roles.Commands.SoftDeleteRole;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Roles.Commands.SoftDeleteRole;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

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
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new SoftDeleteRoleCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new SoftDeleteRoleCommand(role.Id),
            CancellationToken.None);

        result.Should().BeTrue();
        var updated = await dbContext.Roles.IgnoreQueryFilters().FirstAsync(x => x.Id == role.Id);
        updated.DeletedAt.Should().NotBeNull();
        searchIndex.Verify(x => x.DeleteRoleAsync(role.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentRole_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new SoftDeleteRoleCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new SoftDeleteRoleCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.Should().BeFalse();
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
