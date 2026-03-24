namespace Auth.IntegrationTests;

[Collection("Integration")]
public sealed class LoginControllerTests(IntegrationTestFixture fixture)
{
    private HttpClient Client => fixture.Client;

    [Fact]
    public async Task Login_ValidCredentials_ReturnsRedirectUrl()
    {
        var request = new LoginRequest("admin", "admin", "/");

        var response = await Client.PostAsJsonAsync("/connect/login", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(IntegrationTestFixture.JsonOptions);
        result.GetProperty("redirect_url").GetString().Should().Be("/");
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        var request = new LoginRequest("admin", "wrong-password", "/");

        var response = await Client.PostAsJsonAsync("/connect/login", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(IntegrationTestFixture.JsonOptions);
        result.GetProperty("error").GetString().Should().Be("invalid_credentials");
    }

    [Fact]
    public async Task Login_InactiveUser_Returns401()
    {
        var user = await fixture.CreateUserAsync(isActive: false, password: "Test1234!");

        var request = new LoginRequest(user.Username, "Test1234!", "/");

        var response = await Client.PostAsJsonAsync("/connect/login", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_MustChangePassword_ReturnsPasswordChangeRequired()
    {
        var password = "Test1234!";
        var user = await fixture.CreateUserAsync(password: password, mustChangePassword: true);

        // Assign to workspace
        var role = await fixture.CreateRoleAsync();
        var wsResponse = await Client.GetAsync("/api/workspaces");
        var workspaces = await wsResponse.Content
            .ReadFromJsonAsync<IReadOnlyCollection<WorkspaceDto>>(IntegrationTestFixture.JsonOptions);
        var systemWs = workspaces!.First(w => w.Code == "system");
        var wsRequest = new SetUserWorkspacesRequest([
            new UserWorkspaceRolesItem(systemWs.Id, [role.Id])
        ]);
        await Client.PutAsJsonAsync($"/api/users/{user.Id}/workspaces", wsRequest, IntegrationTestFixture.JsonOptions);

        var request = new LoginRequest(user.Username, password, "/");
        var response = await Client.PostAsJsonAsync("/connect/login", request, IntegrationTestFixture.JsonOptions);

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(IntegrationTestFixture.JsonOptions);
        result.GetProperty("error").GetString().Should().Be("password_change_required");
        result.TryGetProperty("challenge_id", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ClientInfo_ExistingClient_ReturnsInfo()
    {
        var response = await Client.GetAsync("/connect/client-info?client_id=system-app");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
