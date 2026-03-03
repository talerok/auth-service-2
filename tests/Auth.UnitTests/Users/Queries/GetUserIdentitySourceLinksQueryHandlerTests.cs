using Auth.Application;
using Auth.Application.Users.Queries.GetUserIdentitySourceLinks;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Users.Queries.GetUserIdentitySourceLinks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Auth.UnitTests.Users.Queries;

public sealed class GetUserIdentitySourceLinksQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetUserIdentitySourceLinksQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetUserIdentitySourceLinksQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenUserHasNoLinks_ReturnsEmptyCollection()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var handler = new GetUserIdentitySourceLinksQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetUserIdentitySourceLinksQuery(user.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenUserHasLinks_ReturnsLinksWithSourceDetails()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        var source = new IdentitySource { Name = "keycloak", DisplayName = "Keycloak", Type = IdentitySourceType.Oidc, IsEnabled = true };
        dbContext.Users.Add(user);
        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync();
        dbContext.IdentitySourceLinks.Add(new IdentitySourceLink
        {
            UserId = user.Id,
            IdentitySourceId = source.Id,
            ExternalIdentity = "ext-sub-123"
        });
        await dbContext.SaveChangesAsync();
        var handler = new GetUserIdentitySourceLinksQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetUserIdentitySourceLinksQuery(user.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        var link = result!.Single();
        link.IdentitySourceId.Should().Be(source.Id);
        link.IdentitySourceName.Should().Be("keycloak");
        link.IdentitySourceDisplayName.Should().Be("Keycloak");
        link.IdentitySourceType.Should().Be(IdentitySourceType.Oidc);
        link.ExternalIdentity.Should().Be("ext-sub-123");
    }

    [Fact]
    public async Task Handle_WhenUserHasMultipleLinks_ReturnsAll()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        var source1 = new IdentitySource { Name = "keycloak", DisplayName = "Keycloak", Type = IdentitySourceType.Oidc, IsEnabled = true };
        var source2 = new IdentitySource { Name = "corporate-ldap", DisplayName = "Corporate LDAP", Type = IdentitySourceType.Ldap, IsEnabled = true };
        dbContext.Users.Add(user);
        dbContext.IdentitySources.AddRange(source1, source2);
        await dbContext.SaveChangesAsync();
        dbContext.IdentitySourceLinks.AddRange(
            new IdentitySourceLink { UserId = user.Id, IdentitySourceId = source1.Id, ExternalIdentity = "oidc-sub" },
            new IdentitySourceLink { UserId = user.Id, IdentitySourceId = source2.Id, ExternalIdentity = "ldap-uid" });
        await dbContext.SaveChangesAsync();
        var handler = new GetUserIdentitySourceLinksQueryHandler(dbContext);

        var result = await handler.Handle(
            new GetUserIdentitySourceLinksQuery(user.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result!.Select(x => x.IdentitySourceName).Should().BeEquivalentTo("keycloak", "corporate-ldap");
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
