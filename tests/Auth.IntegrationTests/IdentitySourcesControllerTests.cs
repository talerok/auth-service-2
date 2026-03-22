using Auth.Domain;

namespace Auth.IntegrationTests;

[Collection("Integration")]
public sealed class IdentitySourcesControllerTests(IntegrationTestFixture fixture)
{
    private HttpClient Client => fixture.Client;

    // --- Auth ---

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        fixture.ClearAuth();
        try
        {
            var response = await Client.GetAsync("/api/identity-sources");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    // --- GetAll ---

    [Fact]
    public async Task GetAll_AsAdmin_ReturnsIdentitySources()
    {
        var response = await Client.GetAsync("/api/identity-sources");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // --- Create OIDC ---

    [Fact]
    public async Task Create_OidcSource_ReturnsDetailDto()
    {
        var name = $"oidc-{Guid.NewGuid():N}";
        var request = new CreateIdentitySourceRequest(
            Name: name,
            Code: name,
            DisplayName: $"OIDC {name}",
            Type: IdentitySourceType.Oidc,
            OidcConfig: new CreateOidcConfigRequest(
                Authority: "https://accounts.google.com",
                ClientId: "test-client-id",
                ClientSecret: "test-client-secret"));

        var response = await Client.PostAsJsonAsync("/api/identity-sources", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content
            .ReadFromJsonAsync<IdentitySourceDetailDto>(IntegrationTestFixture.JsonOptions);
        result!.Name.Should().Be(name);
        result.Type.Should().Be(IdentitySourceType.Oidc);
        result.OidcConfig.Should().NotBeNull();
        result.OidcConfig!.Authority.Should().Be("https://accounts.google.com");
    }

    // --- Create LDAP ---

    [Fact]
    public async Task Create_LdapSource_ReturnsDetailDto()
    {
        var name = $"ldap-{Guid.NewGuid():N}";
        var request = new CreateIdentitySourceRequest(
            Name: name,
            Code: name,
            DisplayName: $"LDAP {name}",
            Type: IdentitySourceType.Ldap,
            LdapConfig: new CreateLdapConfigRequest(
                Host: "ldap.example.com",
                Port: 389,
                BaseDn: "dc=example,dc=com",
                BindDn: "cn=admin,dc=example,dc=com",
                BindPassword: "admin"));

        var response = await Client.PostAsJsonAsync("/api/identity-sources", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content
            .ReadFromJsonAsync<IdentitySourceDetailDto>(IntegrationTestFixture.JsonOptions);
        result!.Type.Should().Be(IdentitySourceType.Ldap);
        result.LdapConfig.Should().NotBeNull();
    }

    // --- GetById ---

    [Fact]
    public async Task GetById_ExistingSource_Returns200()
    {
        var name = $"get-oidc-{Guid.NewGuid():N}";
        var createRequest = new CreateIdentitySourceRequest(
            name, name, $"Test {name}", IdentitySourceType.Oidc,
            OidcConfig: new CreateOidcConfigRequest("https://example.com", "client-id"));
        var createResponse = await Client.PostAsJsonAsync(
            "/api/identity-sources", createRequest, IntegrationTestFixture.JsonOptions);
        var created = await createResponse.Content
            .ReadFromJsonAsync<IdentitySourceDetailDto>(IntegrationTestFixture.JsonOptions);

        var response = await Client.GetAsync($"/api/identity-sources/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content
            .ReadFromJsonAsync<IdentitySourceDetailDto>(IntegrationTestFixture.JsonOptions);
        result!.Id.Should().Be(created.Id);
    }

    // --- Update ---

    [Fact]
    public async Task Update_ExistingSource_ReturnsUpdated()
    {
        var name = $"upd-oidc-{Guid.NewGuid():N}";
        var createRequest = new CreateIdentitySourceRequest(
            name, name, $"Test {name}", IdentitySourceType.Oidc,
            OidcConfig: new CreateOidcConfigRequest("https://example.com", "client-id"));
        var createResponse = await Client.PostAsJsonAsync(
            "/api/identity-sources", createRequest, IntegrationTestFixture.JsonOptions);
        var created = await createResponse.Content
            .ReadFromJsonAsync<IdentitySourceDetailDto>(IntegrationTestFixture.JsonOptions);

        var updateRequest = new UpdateIdentitySourceRequest(
            Code: name,
            DisplayName: "Updated Display Name",
            IsEnabled: true,
            OidcConfig: new CreateOidcConfigRequest("https://updated.example.com", "updated-client-id"));

        var response = await Client.PutAsJsonAsync(
            $"/api/identity-sources/{created!.Id}", updateRequest, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content
            .ReadFromJsonAsync<IdentitySourceDetailDto>(IntegrationTestFixture.JsonOptions);
        updated!.DisplayName.Should().Be("Updated Display Name");
    }

    // --- Delete ---

    [Fact]
    public async Task Delete_ExistingSource_Returns204()
    {
        var name = $"del-oidc-{Guid.NewGuid():N}";
        var createRequest = new CreateIdentitySourceRequest(
            name, name, $"Test {name}", IdentitySourceType.Oidc,
            OidcConfig: new CreateOidcConfigRequest("https://example.com", "client-id"));
        var createResponse = await Client.PostAsJsonAsync(
            "/api/identity-sources", createRequest, IntegrationTestFixture.JsonOptions);
        var created = await createResponse.Content
            .ReadFromJsonAsync<IdentitySourceDetailDto>(IntegrationTestFixture.JsonOptions);

        var response = await Client.DeleteAsync($"/api/identity-sources/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
