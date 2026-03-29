using Auth.Application;
using Auth.Application.Roles.Commands.CreateRole;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Roles.Commands.CreateRole;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Roles.Commands;

public sealed class CreateRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_CreatesRoleAndIndexes()
    {
        await using var dbContext = CreateDbContext();
        var eventBus = new Mock<IEventBus>();
        var handler = new CreateRoleCommandHandler(dbContext, eventBus.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new CreateRoleCommand("Admin", "admin", "Administrator role"),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("Admin");
        result.Code.Should().Be("admin");
        result.Description.Should().Be("Administrator role");

        var entity = await dbContext.Roles.FirstAsync(x => x.Id == result.Id);
        entity.Name.Should().Be("Admin");
        entity.Code.Should().Be("admin");
        entity.Description.Should().Be("Administrator role");

    }

}
