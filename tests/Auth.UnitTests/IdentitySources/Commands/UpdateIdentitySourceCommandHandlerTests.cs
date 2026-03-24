using Auth.Application;
using Auth.Application.IdentitySources.Commands.UpdateIdentitySource;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.IdentitySources.Commands.UpdateIdentitySource;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.IdentitySources.Commands;

public sealed class UpdateIdentitySourceCommandHandlerTests
{
    private const string TestEncryptionKey = "test-encryption-key-min-32-chars-long!";

    private static IOptions<IntegrationOptions> CreateOptions() =>
        Options.Create(new IntegrationOptions { EncryptionKey = TestEncryptionKey });
    [Fact]
    public async Task Handle_UpdatesDisplayNameAndIsEnabled()
    {
        await using var dbContext = CreateDbContext();
        var source = new IdentitySource
        {
            Name = "keycloak", Code = "keycloak", DisplayName = "Old Name", Type = IdentitySourceType.Oidc, IsEnabled = true,
            OidcConfig = new IdentitySourceOidcConfig { Authority = "https://idp.example.com", ClientId = "my-client" }
        };
        source.OidcConfig.IdentitySourceId = source.Id;
        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync();
        var handler = new UpdateIdentitySourceCommandHandler(dbContext, CreateOptions());

        var result = await handler.Handle(
            new UpdateIdentitySourceCommand(source.Id, "keycloak-updated", "New Name", false,
                new CreateOidcConfigRequest("https://new-idp.example.com", "new-client")),
            CancellationToken.None);

        result.Code.Should().Be("keycloak-updated");
        result.DisplayName.Should().Be("New Name");
        result.IsEnabled.Should().BeFalse();
        result.OidcConfig!.Authority.Should().Be("https://new-idp.example.com");
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var handler = new UpdateIdentitySourceCommandHandler(dbContext, CreateOptions());

        var act = () => handler.Handle(
            new UpdateIdentitySourceCommand(Guid.NewGuid(), "code", "Name", true),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceNotFound);
    }

    [Fact]
    public async Task Handle_LdapSource_UpdatesConfig()
    {
        await using var dbContext = CreateDbContext();
        var source = new IdentitySource
        {
            Name = "corporate-ldap", Code = "corporate-ldap", DisplayName = "Old LDAP", Type = IdentitySourceType.Ldap, IsEnabled = true,
            LdapConfig = new IdentitySourceLdapConfig
            {
                Host = "ldap.example.com", Port = 389, BaseDn = "dc=example,dc=com",
                BindDn = "cn=admin,dc=example,dc=com", BindPassword = "secret",
                UseSsl = false, SearchFilter = "(uid={username})"
            }
        };
        source.LdapConfig.IdentitySourceId = source.Id;
        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync();
        var handler = new UpdateIdentitySourceCommandHandler(dbContext, CreateOptions());

        var result = await handler.Handle(
            new UpdateIdentitySourceCommand(source.Id, "corporate-ldap-v2", "New LDAP", false,
                LdapConfig: new CreateLdapConfigRequest("new-ldap.example.com", 636, "dc=new,dc=com", "cn=admin,dc=new,dc=com", UseSsl: true)),
            CancellationToken.None);

        result.DisplayName.Should().Be("New LDAP");
        result.LdapConfig.Should().NotBeNull();
        result.LdapConfig!.Host.Should().Be("new-ldap.example.com");
        result.LdapConfig.UseSsl.Should().BeTrue();
    }

}
