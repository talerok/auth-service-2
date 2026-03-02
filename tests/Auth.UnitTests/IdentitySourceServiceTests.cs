using Auth.Application;
using Auth.Domain;
using Auth.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Auth.UnitTests;

public sealed class IdentitySourceServiceTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsAllSources()
    {
        await using var dbContext = CreateDbContext();
        dbContext.IdentitySources.Add(new IdentitySource
        {
            Name = "keycloak",
            DisplayName = "Keycloak",
            Type = IdentitySourceType.Oidc,
            IsEnabled = true
        });
        await dbContext.SaveChangesAsync();

        var service = new IdentitySourceService(dbContext);

        var result = await service.GetAllAsync(CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().Name.Should().Be("keycloak");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDetailWithOidcConfig()
    {
        await using var dbContext = CreateDbContext();
        var source = new IdentitySource
        {
            Name = "keycloak",
            DisplayName = "Keycloak",
            Type = IdentitySourceType.Oidc,
            IsEnabled = true,
            OidcConfig = new IdentitySourceOidcConfig
            {
                Authority = "https://idp.example.com",
                ClientId = "my-client",
                ClientSecret = "secret"
            }
        };
        source.OidcConfig.IdentitySourceId = source.Id;
        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync();

        var service = new IdentitySourceService(dbContext);

        var result = await service.GetByIdAsync(source.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.OidcConfig.Should().NotBeNull();
        result.OidcConfig!.Authority.Should().Be("https://idp.example.com");
        result.OidcConfig.HasClientSecret.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var service = new IdentitySourceService(dbContext);

        var result = await service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WithOidcConfig_CreatesSourceAndConfig()
    {
        await using var dbContext = CreateDbContext();
        var service = new IdentitySourceService(dbContext);

        var result = await service.CreateAsync(new CreateIdentitySourceRequest(
            "keycloak", "Keycloak", IdentitySourceType.Oidc,
            new CreateOidcConfigRequest("https://idp.example.com", "my-client", "secret")),
            CancellationToken.None);

        result.Name.Should().Be("keycloak");
        result.OidcConfig.Should().NotBeNull();
        result.OidcConfig!.Authority.Should().Be("https://idp.example.com");
        (await dbContext.IdentitySources.CountAsync()).Should().Be(1);
        (await dbContext.IdentitySourceOidcConfigs.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_OidcTypeWithoutConfig_ThrowsTypeMismatch()
    {
        await using var dbContext = CreateDbContext();
        var service = new IdentitySourceService(dbContext);

        var act = () => service.CreateAsync(
            new CreateIdentitySourceRequest("keycloak", "Keycloak", IdentitySourceType.Oidc),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceTypeMismatch);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesDisplayNameAndIsEnabled()
    {
        await using var dbContext = CreateDbContext();
        var source = new IdentitySource
        {
            Name = "keycloak",
            DisplayName = "Old Name",
            Type = IdentitySourceType.Oidc,
            IsEnabled = true,
            OidcConfig = new IdentitySourceOidcConfig
            {
                Authority = "https://idp.example.com",
                ClientId = "my-client"
            }
        };
        source.OidcConfig.IdentitySourceId = source.Id;
        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync();

        var service = new IdentitySourceService(dbContext);

        var result = await service.UpdateAsync(source.Id, new UpdateIdentitySourceRequest(
            "New Name", false,
            new CreateOidcConfigRequest("https://new-idp.example.com", "new-client")),
            CancellationToken.None);

        result.DisplayName.Should().Be("New Name");
        result.IsEnabled.Should().BeFalse();
        result.OidcConfig!.Authority.Should().Be("https://new-idp.example.com");
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var service = new IdentitySourceService(dbContext);

        var act = () => service.UpdateAsync(Guid.NewGuid(),
            new UpdateIdentitySourceRequest("Name", true), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceNotFound);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesSource()
    {
        await using var dbContext = CreateDbContext();
        var source = new IdentitySource
        {
            Name = "keycloak",
            DisplayName = "Keycloak",
            Type = IdentitySourceType.Oidc,
            IsEnabled = true
        };
        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync();

        var service = new IdentitySourceService(dbContext);

        await service.DeleteAsync(source.Id, CancellationToken.None);

        var deleted = await dbContext.IdentitySources.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == source.Id);
        deleted.Should().NotBeNull();
        deleted!.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var service = new IdentitySourceService(dbContext);

        var act = () => service.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceNotFound);
    }

    [Fact]
    public async Task CreateLinkAsync_CreatesLink()
    {
        await using var dbContext = CreateDbContext();
        var source = new IdentitySource
        {
            Name = "keycloak",
            DisplayName = "Keycloak",
            Type = IdentitySourceType.Oidc,
            IsEnabled = true
        };
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            IsActive = true
        };
        dbContext.IdentitySources.Add(source);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = new IdentitySourceService(dbContext);

        var result = await service.CreateLinkAsync(source.Id,
            new CreateIdentitySourceLinkRequest(user.Id, "ext-sub-123"),
            CancellationToken.None);

        result.ExternalIdentity.Should().Be("ext-sub-123");
        result.UserId.Should().Be(user.Id);
        (await dbContext.IdentitySourceLinks.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateLinkAsync_DuplicateLink_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var source = new IdentitySource
        {
            Name = "keycloak",
            DisplayName = "Keycloak",
            Type = IdentitySourceType.Oidc,
            IsEnabled = true
        };
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            IsActive = true
        };
        dbContext.IdentitySources.Add(source);
        dbContext.Users.Add(user);
        dbContext.IdentitySourceLinks.Add(new IdentitySourceLink
        {
            UserId = user.Id,
            IdentitySourceId = source.Id,
            ExternalIdentity = "ext-sub-123"
        });
        await dbContext.SaveChangesAsync();

        var service = new IdentitySourceService(dbContext);

        var act = () => service.CreateLinkAsync(source.Id,
            new CreateIdentitySourceLinkRequest(user.Id, "ext-sub-123"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceDuplicateLink);
    }

    [Fact]
    public async Task GetLinksAsync_ReturnsLinksForSource()
    {
        await using var dbContext = CreateDbContext();
        var source = new IdentitySource
        {
            Name = "keycloak",
            DisplayName = "Keycloak",
            Type = IdentitySourceType.Oidc,
            IsEnabled = true
        };
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            IsActive = true
        };
        dbContext.IdentitySources.Add(source);
        dbContext.Users.Add(user);
        dbContext.IdentitySourceLinks.Add(new IdentitySourceLink
        {
            UserId = user.Id,
            IdentitySourceId = source.Id,
            ExternalIdentity = "ext-sub-123"
        });
        await dbContext.SaveChangesAsync();

        var service = new IdentitySourceService(dbContext);

        var result = await service.GetLinksAsync(source.Id, CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().ExternalIdentity.Should().Be("ext-sub-123");
    }

    [Fact]
    public async Task DeleteLinkAsync_RemovesLink()
    {
        await using var dbContext = CreateDbContext();
        var source = new IdentitySource
        {
            Name = "keycloak",
            DisplayName = "Keycloak",
            Type = IdentitySourceType.Oidc,
            IsEnabled = true
        };
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            IsActive = true
        };
        var link = new IdentitySourceLink
        {
            UserId = user.Id,
            IdentitySourceId = source.Id,
            ExternalIdentity = "ext-sub-123"
        };
        dbContext.IdentitySources.Add(source);
        dbContext.Users.Add(user);
        dbContext.IdentitySourceLinks.Add(link);
        await dbContext.SaveChangesAsync();

        var service = new IdentitySourceService(dbContext);

        await service.DeleteLinkAsync(source.Id, link.Id, CancellationToken.None);

        (await dbContext.IdentitySourceLinks.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DeleteLinkAsync_WhenNotFound_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var source = new IdentitySource
        {
            Name = "keycloak",
            DisplayName = "Keycloak",
            Type = IdentitySourceType.Oidc,
            IsEnabled = true
        };
        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync();

        var service = new IdentitySourceService(dbContext);

        var act = () => service.DeleteLinkAsync(source.Id, Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceLinkNotFound);
    }

    [Fact]
    public async Task CreateAsync_WithLdapConfig_CreatesSourceAndConfig()
    {
        await using var dbContext = CreateDbContext();
        var service = new IdentitySourceService(dbContext);

        var result = await service.CreateAsync(new CreateIdentitySourceRequest(
            "corporate-ldap", "Corporate LDAP", IdentitySourceType.Ldap,
            LdapConfig: new CreateLdapConfigRequest("ldap.example.com", 389, "dc=example,dc=com", "cn=admin,dc=example,dc=com", "secret")),
            CancellationToken.None);

        result.Name.Should().Be("corporate-ldap");
        result.LdapConfig.Should().NotBeNull();
        result.LdapConfig!.Host.Should().Be("ldap.example.com");
        result.LdapConfig.Port.Should().Be(389);
        result.OidcConfig.Should().BeNull();
        (await dbContext.IdentitySources.CountAsync()).Should().Be(1);
        (await dbContext.IdentitySourceLdapConfigs.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_LdapTypeWithoutConfig_ThrowsTypeMismatch()
    {
        await using var dbContext = CreateDbContext();
        var service = new IdentitySourceService(dbContext);

        var act = () => service.CreateAsync(
            new CreateIdentitySourceRequest("ldap", "LDAP", IdentitySourceType.Ldap),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceTypeMismatch);
    }

    [Fact]
    public async Task GetByIdAsync_LdapSource_ReturnsDetailWithLdapConfig()
    {
        await using var dbContext = CreateDbContext();
        var source = new IdentitySource
        {
            Name = "corporate-ldap",
            DisplayName = "Corporate LDAP",
            Type = IdentitySourceType.Ldap,
            IsEnabled = true,
            LdapConfig = new IdentitySourceLdapConfig
            {
                Host = "ldap.example.com",
                Port = 636,
                BaseDn = "dc=example,dc=com",
                BindDn = "cn=admin,dc=example,dc=com",
                BindPassword = "secret",
                UseSsl = true,
                SearchFilter = "(uid={username})"
            }
        };
        source.LdapConfig.IdentitySourceId = source.Id;
        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync();

        var service = new IdentitySourceService(dbContext);

        var result = await service.GetByIdAsync(source.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.LdapConfig.Should().NotBeNull();
        result.LdapConfig!.Host.Should().Be("ldap.example.com");
        result.LdapConfig.Port.Should().Be(636);
        result.LdapConfig.UseSsl.Should().BeTrue();
        result.OidcConfig.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_LdapSource_UpdatesConfig()
    {
        await using var dbContext = CreateDbContext();
        var source = new IdentitySource
        {
            Name = "corporate-ldap",
            DisplayName = "Old LDAP",
            Type = IdentitySourceType.Ldap,
            IsEnabled = true,
            LdapConfig = new IdentitySourceLdapConfig
            {
                Host = "ldap.example.com",
                Port = 389,
                BaseDn = "dc=example,dc=com",
                BindDn = "cn=admin,dc=example,dc=com",
                BindPassword = "secret",
                UseSsl = false,
                SearchFilter = "(uid={username})"
            }
        };
        source.LdapConfig.IdentitySourceId = source.Id;
        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync();

        var service = new IdentitySourceService(dbContext);

        var result = await service.UpdateAsync(source.Id, new UpdateIdentitySourceRequest(
            "New LDAP", false,
            LdapConfig: new CreateLdapConfigRequest("new-ldap.example.com", 636, "dc=new,dc=com", "cn=admin,dc=new,dc=com", UseSsl: true)),
            CancellationToken.None);

        result.DisplayName.Should().Be("New LDAP");
        result.IsEnabled.Should().BeFalse();
        result.LdapConfig.Should().NotBeNull();
        result.LdapConfig!.Host.Should().Be("new-ldap.example.com");
        result.LdapConfig.Port.Should().Be(636);
        result.LdapConfig.UseSsl.Should().BeTrue();
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AuthDbContext(options);
    }
}
