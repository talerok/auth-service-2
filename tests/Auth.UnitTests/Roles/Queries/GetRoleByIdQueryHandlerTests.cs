using Auth.Application;
using Auth.Application.Roles.Queries.GetRoleById;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Roles.Queries.GetRoleById;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Roles.Queries;

public sealed class GetRoleByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ExistingRole_ReturnsDto()
    {
        await using var dbContext = CreateDbContext();
        var role = new Role { Name = "Admin", Code = "admin", Description = "Administrator" };
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();
        var handler = new GetRoleByIdQueryHandler(dbContext);

        var result = await handler.Handle(new GetRoleByIdQuery(role.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Admin");
        result.Description.Should().Be("Administrator");
    }

    [Fact]
    public async Task Handle_NonExistentRole_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetRoleByIdQueryHandler(dbContext);

        var result = await handler.Handle(new GetRoleByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

}
