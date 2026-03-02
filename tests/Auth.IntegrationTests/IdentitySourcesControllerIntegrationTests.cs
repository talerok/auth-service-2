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
    public async Task Links_CrudFlow()
    {
        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);

        // Create a user for linking
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var createUserResponse = await fixture.Client.PostAsJsonAsync("/api/users", new
        {
            username = $"user_{suffix}",
            fullName = $"User {suffix}",
            email = $"user_{suffix}@example.com",
            password = "strong-password",
            isActive = true
        });
        createUserResponse.EnsureSuccessStatusCode();
        var user = await createUserResponse.Content.ReadFromJsonAsync<UserDto>(IntegrationTestFixture.JsonOptions);

        // Create identity source
        var createSourceResponse = await fixture.Client.PostAsJsonAsync("/api/identity-sources", new
        {
            name = $"keycloak-{suffix}",
            displayName = "Keycloak",
            type = "oidc",
            oidcConfig = new
            {
                authority = "https://idp.example.com",
                clientId = "my-client"
            }
        });
        createSourceResponse.EnsureSuccessStatusCode();
        var source = await createSourceResponse.Content.ReadFromJsonAsync<IdentitySourceDetailDto>(IntegrationTestFixture.JsonOptions);

        // Create link
        var createLinkResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/identity-sources/{source!.Id}/links",
            new { userId = user!.Id, externalIdentity = "ext-sub-123" });
        createLinkResponse.IsSuccessStatusCode.Should().BeTrue();
        var link = await createLinkResponse.Content.ReadFromJsonAsync<IdentitySourceLinkDto>(IntegrationTestFixture.JsonOptions);
        link.Should().NotBeNull();
        link!.ExternalIdentity.Should().Be("ext-sub-123");
        link.UserId.Should().Be(user.Id);

        // Get links
        var getLinksResponse = await fixture.Client.GetAsync($"/api/identity-sources/{source.Id}/links");
        getLinksResponse.IsSuccessStatusCode.Should().BeTrue();
        var links = await getLinksResponse.Content.ReadFromJsonAsync<IdentitySourceLinkDto[]>(IntegrationTestFixture.JsonOptions);
        links.Should().HaveCount(1);

        // Delete link
        var deleteLinkResponse = await fixture.Client.DeleteAsync(
            $"/api/identity-sources/{source.Id}/links/{link.Id}");
        deleteLinkResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deleted
        var getLinksAfterDeleteResponse = await fixture.Client.GetAsync($"/api/identity-sources/{source.Id}/links");
        getLinksAfterDeleteResponse.IsSuccessStatusCode.Should().BeTrue();
        var linksAfterDelete = await getLinksAfterDeleteResponse.Content.ReadFromJsonAsync<IdentitySourceLinkDto[]>(IntegrationTestFixture.JsonOptions);
        linksAfterDelete.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateLink_DuplicateExternalIdentity_ReturnsBadRequest()
    {
        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var createUserResponse = await fixture.Client.PostAsJsonAsync("/api/users", new
        {
            username = $"user_{suffix}",
            fullName = $"User {suffix}",
            email = $"user_{suffix}@example.com",
            password = "strong-password",
            isActive = true
        });
        createUserResponse.EnsureSuccessStatusCode();
        var user = await createUserResponse.Content.ReadFromJsonAsync<UserDto>(IntegrationTestFixture.JsonOptions);

        var createSourceResponse = await fixture.Client.PostAsJsonAsync("/api/identity-sources", new
        {
            name = $"keycloak-{suffix}",
            displayName = "Keycloak",
            type = "oidc",
            oidcConfig = new
            {
                authority = "https://idp.example.com",
                clientId = "my-client"
            }
        });
        createSourceResponse.EnsureSuccessStatusCode();
        var source = await createSourceResponse.Content.ReadFromJsonAsync<IdentitySourceDetailDto>(IntegrationTestFixture.JsonOptions);

        // First link — should succeed
        var firstLink = await fixture.Client.PostAsJsonAsync(
            $"/api/identity-sources/{source!.Id}/links",
            new { userId = user!.Id, externalIdentity = "same-ext-sub" });
        firstLink.IsSuccessStatusCode.Should().BeTrue();

        // Duplicate link — should fail
        var duplicateLink = await fixture.Client.PostAsJsonAsync(
            $"/api/identity-sources/{source.Id}/links",
            new { userId = user.Id, externalIdentity = "same-ext-sub" });
        duplicateLink.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
            password = "password123",
            isActive = true
        });
        createResponse.EnsureSuccessStatusCode();

        fixture.ClearAuth();
        var userTokens = await fixture.LoginAsync(username, "password123");
        fixture.SetBearerToken(userTokens.AccessToken);

        var response = await fixture.Client.PostAsJsonAsync("/api/identity-sources", new
        {
            name = "test-source",
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
}
