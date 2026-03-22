using Auth.Domain;
using Microsoft.EntityFrameworkCore;

namespace Auth.IntegrationTests;

[Collection("Integration")]
public sealed class AccountControllerTests(IntegrationTestFixture fixture)
{
    private HttpClient Client => fixture.Client;

    [Fact]
    public async Task GetPasswordRequirements_ReturnsOptions()
    {
        fixture.ClearAuth();
        try
        {
            var response = await Client.GetAsync("/api/account/password-requirements");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var options = await response.Content
                .ReadFromJsonAsync<JsonElement>(IntegrationTestFixture.JsonOptions);
            options.GetProperty("minLength").GetInt32().Should().BeGreaterThan(0);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    [Fact]
    public async Task EnableTwoFactor_WithoutToken_Returns401()
    {
        fixture.ClearAuth();
        try
        {
            var request = new EnableTwoFactorRequest(TwoFactorChannel.Email);
            var response = await Client.PostAsJsonAsync("/api/account/2fa/enable", request, IntegrationTestFixture.JsonOptions);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    [Fact]
    public async Task EnableTwoFactor_Authorized_Returns200()
    {
        // Create a user specifically for 2FA testing
        var password = "Test1234!";
        var user = await fixture.CreateUserAsync(password: password);

        // Assign user to system workspace with a role so they can get a valid token
        var role = await fixture.CreateRoleAsync();
        var wsResponse = await Client.GetAsync("/api/workspaces");
        var workspaces = await wsResponse.Content
            .ReadFromJsonAsync<IReadOnlyCollection<WorkspaceDto>>(IntegrationTestFixture.JsonOptions);
        var systemWs = workspaces!.First(w => w.Code == "system");
        var wsRequest = new SetUserWorkspacesRequest([
            new UserWorkspaceRolesItem(systemWs.Id, [role.Id])
        ]);
        await Client.PutAsJsonAsync($"/api/users/{user.Id}/workspaces", wsRequest, IntegrationTestFixture.JsonOptions);

        var token = await fixture.AcquireTokenAsync(user.Username, password);
        fixture.SetBearerToken(token);
        try
        {
            var request = new EnableTwoFactorRequest(TwoFactorChannel.Email);
            var response = await Client.PostAsJsonAsync("/api/account/2fa/enable", request, IntegrationTestFixture.JsonOptions);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content
                .ReadFromJsonAsync<EnableTwoFactorResponse>(IntegrationTestFixture.JsonOptions);
            result!.ChallengeId.Should().NotBeEmpty();
            result.Channel.Should().Be(TwoFactorChannel.Email);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    [Fact]
    public async Task TwoFactor_EnableAndConfirm_Succeeds()
    {
        var password = "Test1234!";
        var user = await fixture.CreateUserAsync(password: password);

        var role = await fixture.CreateRoleAsync();
        var wsResponse = await Client.GetAsync("/api/workspaces");
        var workspaces = await wsResponse.Content
            .ReadFromJsonAsync<IReadOnlyCollection<WorkspaceDto>>(IntegrationTestFixture.JsonOptions);
        var systemWs = workspaces!.First(w => w.Code == "system");
        var wsRequest = new SetUserWorkspacesRequest([
            new UserWorkspaceRolesItem(systemWs.Id, [role.Id])
        ]);
        await Client.PutAsJsonAsync($"/api/users/{user.Id}/workspaces", wsRequest, IntegrationTestFixture.JsonOptions);

        var token = await fixture.AcquireTokenAsync(user.Username, password);
        fixture.SetBearerToken(token);
        try
        {
            // Enable 2FA
            var enableRequest = new EnableTwoFactorRequest(TwoFactorChannel.Email);
            var enableResponse = await Client.PostAsJsonAsync(
                "/api/account/2fa/enable", enableRequest, IntegrationTestFixture.JsonOptions);
            enableResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var enableResult = await enableResponse.Content
                .ReadFromJsonAsync<EnableTwoFactorResponse>(IntegrationTestFixture.JsonOptions);

            // Mark delivery as completed (background service is removed in tests)
            await fixture.ExecuteDbAsync(async db =>
            {
                var challenge = await db.Set<TwoFactorChallenge>()
                    .FirstAsync(c => c.Id == enableResult!.ChallengeId);
                challenge.MarkDelivered();
                await db.SaveChangesAsync();
            });

            // Confirm with static OTP
            var confirmRequest = new VerifyTwoFactorRequest(
                enableResult!.ChallengeId, TwoFactorChannel.Email, "123456");
            var confirmResponse = await Client.PostAsJsonAsync(
                "/api/account/2fa/confirm", confirmRequest, IntegrationTestFixture.JsonOptions);

            confirmResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    [Fact]
    public async Task TwoFactor_EnableConfirmAndDisable_Succeeds()
    {
        var password = "Test1234!";
        var user = await fixture.CreateUserAsync(password: password);

        var role = await fixture.CreateRoleAsync();
        var wsResponse = await Client.GetAsync("/api/workspaces");
        var workspaces = await wsResponse.Content
            .ReadFromJsonAsync<IReadOnlyCollection<WorkspaceDto>>(IntegrationTestFixture.JsonOptions);
        var systemWs = workspaces!.First(w => w.Code == "system");
        var wsRequest = new SetUserWorkspacesRequest([
            new UserWorkspaceRolesItem(systemWs.Id, [role.Id])
        ]);
        await Client.PutAsJsonAsync($"/api/users/{user.Id}/workspaces", wsRequest, IntegrationTestFixture.JsonOptions);

        var token = await fixture.AcquireTokenAsync(user.Username, password);
        fixture.SetBearerToken(token);
        try
        {
            // Enable
            var enableRequest = new EnableTwoFactorRequest(TwoFactorChannel.Email);
            var enableResponse = await Client.PostAsJsonAsync(
                "/api/account/2fa/enable", enableRequest, IntegrationTestFixture.JsonOptions);
            var enableResult = await enableResponse.Content
                .ReadFromJsonAsync<EnableTwoFactorResponse>(IntegrationTestFixture.JsonOptions);

            // Mark delivery as completed (background service is removed in tests)
            await fixture.ExecuteDbAsync(async db =>
            {
                var challenge = await db.Set<TwoFactorChallenge>()
                    .FirstAsync(c => c.Id == enableResult!.ChallengeId);
                challenge.MarkDelivered();
                await db.SaveChangesAsync();
            });

            // Confirm
            var confirmRequest = new VerifyTwoFactorRequest(
                enableResult!.ChallengeId, TwoFactorChannel.Email, "123456");
            await Client.PostAsJsonAsync("/api/account/2fa/confirm", confirmRequest, IntegrationTestFixture.JsonOptions);

            // Disable
            var disableResponse = await Client.PostAsync("/api/account/2fa/disable", null);

            disableResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    [Fact]
    public async Task ForcedPasswordChange_ValidChallenge_Returns204()
    {
        var password = "Test1234!";
        var user = await fixture.CreateUserAsync(password: password, mustChangePassword: true);

        // Assign to workspace so user can attempt login
        var role = await fixture.CreateRoleAsync();
        var wsResponse = await Client.GetAsync("/api/workspaces");
        var workspaces = await wsResponse.Content
            .ReadFromJsonAsync<IReadOnlyCollection<WorkspaceDto>>(IntegrationTestFixture.JsonOptions);
        var systemWs = workspaces!.First(w => w.Code == "system");
        var wsRequest = new SetUserWorkspacesRequest([
            new UserWorkspaceRolesItem(systemWs.Id, [role.Id])
        ]);
        await Client.PutAsJsonAsync($"/api/users/{user.Id}/workspaces", wsRequest, IntegrationTestFixture.JsonOptions);

        // Login should return password_change_required
        var loginRequest = new LoginRequest(user.Username, password, "/");
        var loginResponse = await Client.PostAsJsonAsync("/connect/login", loginRequest, IntegrationTestFixture.JsonOptions);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<JsonElement>(IntegrationTestFixture.JsonOptions);

        loginResult.GetProperty("error").GetString().Should().Be("password_change_required");
        var challengeId = Guid.Parse(loginResult.GetProperty("challenge_id").GetString()!);

        // Change password using the challenge
        var changeRequest = new ForcedPasswordChangeRequest(challengeId, "NewPass5678!");
        var changeResponse = await Client.PostAsJsonAsync(
            "/api/account/password/forced-change", changeRequest, IntegrationTestFixture.JsonOptions);

        changeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify new password works
        var newToken = await fixture.AcquireTokenAsync(user.Username, "NewPass5678!");
        newToken.Should().NotBeNullOrWhiteSpace();
    }
}
