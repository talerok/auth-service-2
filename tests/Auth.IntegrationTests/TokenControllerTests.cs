namespace Auth.IntegrationTests;

[Collection("Integration")]
public sealed class TokenControllerTests(IntegrationTestFixture fixture)
{
    private HttpClient Client => fixture.Client;

    [Fact]
    public async Task Exchange_PasswordGrant_ValidCredentials_ReturnsAccessToken()
    {
        var request = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = "admin",
            ["password"] = "admin",
            ["client_id"] = "system-app",
            ["scope"] = "openid profile email ws:system offline_access"
        });

        var response = await Client.PostAsync("/connect/token", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(IntegrationTestFixture.JsonOptions);
        json.GetProperty("access_token").GetString().Should().NotBeNullOrWhiteSpace();
        json.GetProperty("refresh_token").GetString().Should().NotBeNullOrWhiteSpace();
        json.GetProperty("token_type").GetString().Should().Be("Bearer");
    }

    [Fact]
    public async Task Exchange_PasswordGrant_InvalidPassword_ReturnsError()
    {
        var request = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = "admin",
            ["password"] = "wrong-password",
            ["client_id"] = "system-app",
            ["scope"] = "openid"
        });

        var response = await Client.PostAsync("/connect/token", request);

        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Exchange_PasswordGrant_NonexistentUser_ReturnsError()
    {
        var request = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = "nonexistent-user",
            ["password"] = "Test1234!",
            ["client_id"] = "system-app",
            ["scope"] = "openid"
        });

        var response = await Client.PostAsync("/connect/token", request);

        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Exchange_PasswordGrant_InactiveUser_ReturnsError()
    {
        var user = await fixture.CreateUserAsync(isActive: false, password: "Test1234!");

        var request = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = user.Username,
            ["password"] = "Test1234!",
            ["client_id"] = "system-app",
            ["scope"] = "openid"
        });

        var response = await Client.PostAsync("/connect/token", request);

        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Exchange_RefreshToken_ValidToken_ReturnsNewAccessToken()
    {
        var (_, refreshToken) = await fixture.AcquireTokenWithRefreshAsync("admin", "admin");

        var request = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = "system-app"
        });

        var response = await Client.PostAsync("/connect/token", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(IntegrationTestFixture.JsonOptions);
        json.GetProperty("access_token").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Exchange_PasswordGrant_WithWsScope_TokenContainsWsClaim()
    {
        var (accessToken, _) = await fixture.AcquireTokenWithRefreshAsync("admin", "admin");

        // Decode the JWT to check the ws claim
        var parts = accessToken.Split('.');
        parts.Should().HaveCount(3);

        var payload = parts[1];
        // Pad base64 if needed
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        var payloadBytes = Convert.FromBase64String(payload);
        var payloadJson = System.Text.Encoding.UTF8.GetString(payloadBytes);
        var claims = JsonSerializer.Deserialize<JsonElement>(payloadJson);

        claims.TryGetProperty("ws:system", out var wsClaim).Should().BeTrue();
        wsClaim.GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Exchange_ClientCredentials_ValidServiceAccount_ReturnsToken()
    {
        var sa = await fixture.CreateServiceAccountAsync();

        // Assign role with permissions to the service account in system workspace
        var role = await fixture.CreateRoleAsync();
        var wsResponse = await Client.GetAsync("/api/workspaces");
        var workspaces = await wsResponse.Content
            .ReadFromJsonAsync<IReadOnlyCollection<WorkspaceDto>>(IntegrationTestFixture.JsonOptions);
        var systemWs = workspaces!.First(w => w.Code == "system");

        var wsRequest = new SetServiceAccountWorkspacesRequest([
            new ServiceAccountWorkspaceRolesItem(systemWs.Id, [role.Id])
        ]);
        await Client.PutAsJsonAsync($"/api/service-accounts/{sa.ServiceAccount.Id}/workspaces",
            wsRequest, IntegrationTestFixture.JsonOptions);

        var request = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = sa.ServiceAccount.ClientId,
            ["client_secret"] = sa.ClientSecret,
            ["scope"] = "openid ws:system"
        });

        var response = await Client.PostAsync("/connect/token", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(IntegrationTestFixture.JsonOptions);
        json.GetProperty("access_token").GetString().Should().NotBeNullOrWhiteSpace();
    }
}
