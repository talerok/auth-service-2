using Auth.Application;
using Auth.Application.IdentitySources.Queries.GetIdentitySourceLinks;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.IdentitySources.Queries.GetIdentitySourceLinks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Auth.UnitTests.IdentitySources.Queries;

public sealed class GetIdentitySourceLinksQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsLinksForSource()
    {
        await using var dbContext = CreateDbContext();
        var source = new IdentitySource { Name = "keycloak", Code = "keycloak", DisplayName = "Keycloak", Type = IdentitySourceType.Oidc, IsEnabled = true };
        var user = new User { Username = "testuser", Email = "test@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.IdentitySources.Add(source);
        dbContext.Users.Add(user);
        dbContext.IdentitySourceLinks.Add(new IdentitySourceLink { UserId = user.Id, IdentitySourceId = source.Id, ExternalIdentity = "ext-sub-123" });
        await dbContext.SaveChangesAsync();
        var handler = new GetIdentitySourceLinksQueryHandler(dbContext);

        var result = await handler.Handle(new GetIdentitySourceLinksQuery(source.Id), CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().ExternalIdentity.Should().Be("ext-sub-123");
    }

    [Fact]
    public async Task Handle_WhenSourceNotFound_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetIdentitySourceLinksQueryHandler(dbContext);

        var act = () => handler.Handle(new GetIdentitySourceLinksQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceNotFound);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
