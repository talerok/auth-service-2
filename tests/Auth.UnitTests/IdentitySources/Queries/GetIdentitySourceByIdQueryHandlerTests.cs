using Auth.Application.IdentitySources.Queries.GetIdentitySourceById;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.IdentitySources.Queries.GetIdentitySourceById;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Auth.UnitTests.IdentitySources.Queries;

public sealed class GetIdentitySourceByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenExists_ReturnsDetailWithOidcConfig()
    {
        await using var dbContext = CreateDbContext();
        var source = new IdentitySource
        {
            Name = "keycloak", DisplayName = "Keycloak", Type = IdentitySourceType.Oidc, IsEnabled = true,
            OidcConfig = new IdentitySourceOidcConfig { Authority = "https://idp.example.com", ClientId = "my-client", ClientSecret = "secret" }
        };
        source.OidcConfig.IdentitySourceId = source.Id;
        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync();
        var handler = new GetIdentitySourceByIdQueryHandler(dbContext);

        var result = await handler.Handle(new GetIdentitySourceByIdQuery(source.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.OidcConfig.Should().NotBeNull();
        result.OidcConfig!.Authority.Should().Be("https://idp.example.com");
        result.OidcConfig.HasClientSecret.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenNotExists_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetIdentitySourceByIdQueryHandler(dbContext);

        var result = await handler.Handle(new GetIdentitySourceByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_LdapSource_ReturnsDetailWithLdapConfig()
    {
        await using var dbContext = CreateDbContext();
        var source = new IdentitySource
        {
            Name = "corporate-ldap", DisplayName = "Corporate LDAP", Type = IdentitySourceType.Ldap, IsEnabled = true,
            LdapConfig = new IdentitySourceLdapConfig
            {
                Host = "ldap.example.com", Port = 636, BaseDn = "dc=example,dc=com",
                BindDn = "cn=admin,dc=example,dc=com", BindPassword = "secret",
                UseSsl = true, SearchFilter = "(uid={username})"
            }
        };
        source.LdapConfig.IdentitySourceId = source.Id;
        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync();
        var handler = new GetIdentitySourceByIdQueryHandler(dbContext);

        var result = await handler.Handle(new GetIdentitySourceByIdQuery(source.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.LdapConfig.Should().NotBeNull();
        result.LdapConfig!.Host.Should().Be("ldap.example.com");
        result.LdapConfig.UseSsl.Should().BeTrue();
        result.OidcConfig.Should().BeNull();
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
