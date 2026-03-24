using Auth.Application.IdentitySources.Queries.GetAllIdentitySources;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.IdentitySources.Queries.GetAllIdentitySources;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.IdentitySources.Queries;

public sealed class GetAllIdentitySourcesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllSources()
    {
        await using var dbContext = CreateDbContext();
        dbContext.IdentitySources.Add(new IdentitySource { Name = "keycloak", Code = "keycloak", DisplayName = "Keycloak", Type = IdentitySourceType.Oidc, IsEnabled = true });
        await dbContext.SaveChangesAsync();
        var handler = new GetAllIdentitySourcesQueryHandler(dbContext);

        var result = await handler.Handle(new GetAllIdentitySourcesQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().Name.Should().Be("keycloak");
    }

}
