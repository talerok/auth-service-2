using Auth.Application;
using Auth.Application.IdentitySources.Commands.CreateIdentitySource;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.IdentitySources.Commands.CreateIdentitySource;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.IdentitySources.Commands;

public sealed class CreateIdentitySourceCommandHandlerTests
{
    private const string TestEncryptionKey = "test-encryption-key-min-32-chars-long!";

    private static IOptions<IntegrationOptions> CreateOptions() =>
        Options.Create(new IntegrationOptions { EncryptionKey = TestEncryptionKey });

    [Fact]
    public async Task Handle_WithOidcConfig_CreatesSourceAndConfig()
    {
        await using var dbContext = CreateDbContext();
        var handler = new CreateIdentitySourceCommandHandler(dbContext, CreateOptions(), new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new CreateIdentitySourceCommand("keycloak", "keycloak", "Keycloak", IdentitySourceType.Oidc,
                new CreateOidcConfigRequest("https://idp.example.com", "my-client", "secret")),
            CancellationToken.None);

        result.Name.Should().Be("keycloak");
        result.Code.Should().Be("keycloak");
        result.OidcConfig.Should().NotBeNull();
        result.OidcConfig!.Authority.Should().Be("https://idp.example.com");
        (await dbContext.IdentitySources.CountAsync()).Should().Be(1);
        (await dbContext.IdentitySourceOidcConfigs.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithLdapConfig_CreatesSourceAndConfig()
    {
        await using var dbContext = CreateDbContext();
        var handler = new CreateIdentitySourceCommandHandler(dbContext, CreateOptions(), new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new CreateIdentitySourceCommand("corporate-ldap", "corporate-ldap", "Corporate LDAP", IdentitySourceType.Ldap,
                LdapConfig: new CreateLdapConfigRequest("ldap.example.com", 389, "dc=example,dc=com", "cn=admin,dc=example,dc=com", "secret")),
            CancellationToken.None);

        result.Name.Should().Be("corporate-ldap");
        result.LdapConfig.Should().NotBeNull();
        result.LdapConfig!.Host.Should().Be("ldap.example.com");
        result.OidcConfig.Should().BeNull();
    }

    [Fact]
    public async Task Handle_OidcTypeWithoutConfig_ThrowsTypeMismatch()
    {
        await using var dbContext = CreateDbContext();
        var handler = new CreateIdentitySourceCommandHandler(dbContext, CreateOptions(), new Mock<IAuditContext>().Object);

        var act = () => handler.Handle(
            new CreateIdentitySourceCommand("keycloak", "keycloak", "Keycloak", IdentitySourceType.Oidc),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceTypeMismatch);
    }

    [Fact]
    public async Task Handle_LdapTypeWithoutConfig_ThrowsTypeMismatch()
    {
        await using var dbContext = CreateDbContext();
        var handler = new CreateIdentitySourceCommandHandler(dbContext, CreateOptions(), new Mock<IAuditContext>().Object);

        var act = () => handler.Handle(
            new CreateIdentitySourceCommand("ldap", "ldap", "LDAP", IdentitySourceType.Ldap),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceTypeMismatch);
    }

}
