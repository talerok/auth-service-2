using Auth.Application;
using Auth.Domain;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Auth.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class IdentitySourcesControllerIntegrationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task GetAll_WithoutAccessToken_ReturnsUnauthorized()
    {
        fixture.ClearAuth();

        var response = await fixture.Client.GetAsync("/api/identity-sources");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithAdminToken_ReturnsOk()
    {
        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);

        var response = await fixture.Client.GetAsync("/api/identity-sources");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_WithAdminToken_ReturnsCreatedSource()
    {
        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var response = await fixture.Client.PostAsJsonAsync("/api/identity-sources", new
        {
            name = $"keycloak-{suffix}",
            code = $"keycloak-{suffix}",
            displayName = "Keycloak Test",
            type = "oidc",
            oidcConfig = new
            {
                authority = "https://idp.example.com",
                clientId = "my-client",
                clientSecret = "secret"
            }
        });

        response.IsSuccessStatusCode.Should().BeTrue();
        var created = await response.Content.ReadFromJsonAsync<IdentitySourceDetailDto>(IntegrationTestFixture.JsonOptions);
        created.Should().NotBeNull();
        created!.Name.Should().Be($"keycloak-{suffix}");
        created.Code.Should().Be($"keycloak-{suffix}");
        created.Type.Should().Be(IdentitySourceType.Oidc);
        created.IsEnabled.Should().BeTrue();
        created.OidcConfig.Should().NotBeNull();
        created.OidcConfig!.Authority.Should().Be("https://idp.example.com");
        created.OidcConfig.HasClientSecret.Should().BeTrue();
    }

    [Fact]
    public async Task GetById_AfterCreate_ReturnsSource()
    {
        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var createResponse = await fixture.Client.PostAsJsonAsync("/api/identity-sources", new
        {
            name = $"keycloak-{suffix}",
            code = $"keycloak-{suffix}",
            displayName = "Keycloak Test",
            type = "oidc",
            oidcConfig = new
            {
                authority = "https://idp.example.com",
                clientId = "my-client"
            }
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<IdentitySourceDetailDto>(IntegrationTestFixture.JsonOptions);

        var response = await fixture.Client.GetAsync($"/api/identity-sources/{created!.Id}");

        response.IsSuccessStatusCode.Should().BeTrue();
        var detail = await response.Content.ReadFromJsonAsync<IdentitySourceDetailDto>(IntegrationTestFixture.JsonOptions);
        detail.Should().NotBeNull();
        detail!.Id.Should().Be(created.Id);
        detail.OidcConfig.Should().NotBeNull();
    }

    [Fact]
    public async Task GetById_WhenNotExists_ReturnsNotFound()
    {
        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);

        var response = await fixture.Client.GetAsync($"/api/identity-sources/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ModifiesSource()
    {
        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var createResponse = await fixture.Client.PostAsJsonAsync("/api/identity-sources", new
        {
            name = $"keycloak-{suffix}",
            code = $"keycloak-{suffix}",
            displayName = "Old Name",
            type = "oidc",
            oidcConfig = new
            {
                authority = "https://idp.example.com",
                clientId = "my-client"
            }
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<IdentitySourceDetailDto>(IntegrationTestFixture.JsonOptions);

        var updateResponse = await fixture.Client.PutAsJsonAsync($"/api/identity-sources/{created!.Id}", new
        {
            code = $"keycloak-{suffix}-updated",
            displayName = "New Name",
            isEnabled = false,
            oidcConfig = new
            {
                authority = "https://new-idp.example.com",
                clientId = "new-client"
            }
        });

        updateResponse.IsSuccessStatusCode.Should().BeTrue();
        var updated = await updateResponse.Content.ReadFromJsonAsync<IdentitySourceDetailDto>(IntegrationTestFixture.JsonOptions);
        updated!.DisplayName.Should().Be("New Name");
        updated.IsEnabled.Should().BeFalse();
        updated.OidcConfig!.Authority.Should().Be("https://new-idp.example.com");
    }

    [Fact]
    public async Task Delete_RemovesSource()
    {
        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var createResponse = await fixture.Client.PostAsJsonAsync("/api/identity-sources", new
        {
            name = $"keycloak-{suffix}",
            code = $"keycloak-{suffix}",
            displayName = "Keycloak",
            type = "oidc",
            oidcConfig = new
            {
                authority = "https://idp.example.com",
                clientId = "my-client"
            }
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<IdentitySourceDetailDto>(IntegrationTestFixture.JsonOptions);

        var deleteResponse = await fixture.Client.DeleteAsync($"/api/identity-sources/{created!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await fixture.Client.GetAsync($"/api/identity-sources/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithUserWithoutPermissions_ReturnsForbidden()
    {
        fixture.ClearAuth();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"user_{suffix}";

        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);
        var createResponse = await fixture.Client.PostAsJsonAsync("/api/users", new
        {
            username,
            fullName = username,
            email = $"{username}@example.com",
            password = "Password1",
            isActive = true
        });
        createResponse.EnsureSuccessStatusCode();

        fixture.ClearAuth();
        var userTokens = await fixture.LoginAsync(username, "Password1");
        fixture.SetBearerToken(userTokens.AccessToken);

        var response = await fixture.Client.PostAsJsonAsync("/api/identity-sources", new
        {
            name = "test-source",
            code = "test-source",
            displayName = "Test",
            type = "oidc",
            oidcConfig = new
            {
                authority = "https://idp.example.com",
                clientId = "my-client"
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_LdapSource_ReturnsCreatedSource()
    {
        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var response = await fixture.Client.PostAsJsonAsync("/api/identity-sources", new
        {
            name = $"ldap-{suffix}",
            code = $"ldap-{suffix}",
            displayName = "Corporate LDAP",
            type = "ldap",
            ldapConfig = new
            {
                host = "ldap.example.com",
                port = 389,
                baseDn = "dc=example,dc=com",
                bindDn = "cn=admin,dc=example,dc=com",
                bindPassword = "secret",
                useSsl = false,
                searchFilter = "(uid={username})"
            }
        });

        response.IsSuccessStatusCode.Should().BeTrue();
        var created = await response.Content.ReadFromJsonAsync<IdentitySourceDetailDto>(IntegrationTestFixture.JsonOptions);
        created.Should().NotBeNull();
        created!.Name.Should().Be($"ldap-{suffix}");
        created.Type.Should().Be(IdentitySourceType.Ldap);
        created.IsEnabled.Should().BeTrue();
        created.LdapConfig.Should().NotBeNull();
        created.LdapConfig!.Host.Should().Be("ldap.example.com");
        created.LdapConfig.Port.Should().Be(389);
        created.OidcConfig.Should().BeNull();
    }

    [Fact]
    public async Task Create_LdapTypeWithoutConfig_ReturnsBadRequest()
    {
        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var response = await fixture.Client.PostAsJsonAsync("/api/identity-sources", new
        {
            name = $"ldap-{suffix}",
            code = $"ldap-{suffix}",
            displayName = "LDAP No Config",
            type = "ldap"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_LdapSource_ReturnsWithLdapConfig()
    {
        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var createResponse = await fixture.Client.PostAsJsonAsync("/api/identity-sources", new
        {
            name = $"ldap-{suffix}",
            code = $"ldap-{suffix}",
            displayName = "Corporate LDAP",
            type = "ldap",
            ldapConfig = new
            {
                host = "ldap.example.com",
                port = 636,
                baseDn = "dc=example,dc=com",
                bindDn = "cn=admin,dc=example,dc=com",
                bindPassword = "secret",
                useSsl = true,
                searchFilter = "(sAMAccountName={username})"
            }
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<IdentitySourceDetailDto>(IntegrationTestFixture.JsonOptions);

        var response = await fixture.Client.GetAsync($"/api/identity-sources/{created!.Id}");

        response.IsSuccessStatusCode.Should().BeTrue();
        var detail = await response.Content.ReadFromJsonAsync<IdentitySourceDetailDto>(IntegrationTestFixture.JsonOptions);
        detail.Should().NotBeNull();
        detail!.LdapConfig.Should().NotBeNull();
        detail.LdapConfig!.Host.Should().Be("ldap.example.com");
        detail.LdapConfig.Port.Should().Be(636);
        detail.LdapConfig.UseSsl.Should().BeTrue();
    }

    [Fact]
    public async Task Update_LdapSource_ModifiesConfig()
    {
        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var createResponse = await fixture.Client.PostAsJsonAsync("/api/identity-sources", new
        {
            name = $"ldap-{suffix}",
            code = $"ldap-{suffix}",
            displayName = "Old LDAP",
            type = "ldap",
            ldapConfig = new
            {
                host = "ldap.example.com",
                port = 389,
                baseDn = "dc=example,dc=com",
                bindDn = "cn=admin,dc=example,dc=com",
                bindPassword = "secret",
                useSsl = false,
                searchFilter = "(uid={username})"
            }
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<IdentitySourceDetailDto>(IntegrationTestFixture.JsonOptions);

        var updateResponse = await fixture.Client.PutAsJsonAsync($"/api/identity-sources/{created!.Id}", new
        {
            code = $"ldap-{suffix}-updated",
            displayName = "New LDAP",
            isEnabled = false,
            ldapConfig = new
            {
                host = "new-ldap.example.com",
                port = 636,
                baseDn = "dc=new,dc=com",
                bindDn = "cn=admin,dc=new,dc=com",
                useSsl = true,
                searchFilter = "(sAMAccountName={username})"
            }
        });

        updateResponse.IsSuccessStatusCode.Should().BeTrue();
        var updated = await updateResponse.Content.ReadFromJsonAsync<IdentitySourceDetailDto>(IntegrationTestFixture.JsonOptions);
        updated!.DisplayName.Should().Be("New LDAP");
        updated.IsEnabled.Should().BeFalse();
        updated.LdapConfig.Should().NotBeNull();
        updated.LdapConfig!.Host.Should().Be("new-ldap.example.com");
        updated.LdapConfig.Port.Should().Be(636);
        updated.LdapConfig.UseSsl.Should().BeTrue();
    }
}
