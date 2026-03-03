using Auth.Application;
using Auth.Application.Roles.Queries.GetAllRoles;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Roles.Queries.GetAllRoles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Auth.UnitTests.Roles.Queries;

public sealed class GetAllRolesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllRoles()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Roles.AddRange(
            new Role { Name = "Admin", Description = "Administrator" },
            new Role { Name = "Reader", Description = "Read-only access" });
        await dbContext.SaveChangesAsync();
        var handler = new GetAllRolesQueryHandler(dbContext);

        var result = await handler.Handle(new GetAllRolesQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(x => x.Name).Should().BeEquivalentTo("Admin", "Reader");
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
