using Auth.Application.IdentitySources.Queries.GetAllIdentitySources;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.IdentitySources.Queries.GetAllIdentitySources;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Auth.UnitTests.IdentitySources.Queries;

public sealed class GetAllIdentitySourcesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllSources()
    {
        await using var dbContext = CreateDbContext();
        dbContext.IdentitySources.Add(new IdentitySource { Name = "keycloak", DisplayName = "Keycloak", Type = IdentitySourceType.Oidc, IsEnabled = true });
        await dbContext.SaveChangesAsync();
        var handler = new GetAllIdentitySourcesQueryHandler(dbContext);

        var result = await handler.Handle(new GetAllIdentitySourcesQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().Name.Should().Be("keycloak");
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
