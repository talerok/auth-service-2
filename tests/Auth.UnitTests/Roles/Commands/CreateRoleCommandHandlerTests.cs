using Auth.Application;
using Auth.Application.Roles.Commands.CreateRole;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Roles.Commands.CreateRole;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Auth.UnitTests.Roles.Commands;

public sealed class CreateRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_CreatesRoleAndIndexes()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        searchIndex.Setup(x => x.IndexRoleAsync(It.IsAny<RoleDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new CreateRoleCommandHandler(dbContext, searchIndex.Object);

        var result = await handler.Handle(
            new CreateRoleCommand("Admin", "Administrator role"),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("Admin");
        result.Description.Should().Be("Administrator role");

        var entity = await dbContext.Roles.FirstAsync(x => x.Id == result.Id);
        entity.Name.Should().Be("Admin");
        entity.Description.Should().Be("Administrator role");

        searchIndex.Verify(x => x.IndexRoleAsync(It.Is<RoleDto>(r => r.Id == result.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
