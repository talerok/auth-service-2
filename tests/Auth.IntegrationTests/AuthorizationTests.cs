namespace Auth.IntegrationTests;

[Collection("Integration")]
public sealed class AuthorizationTests(IntegrationTestFixture fixture)
{
    private HttpClient Client => fixture.Client;

    [Fact]
    public async Task UserWithViewOnly_CannotCreate_Returns403()
    {
        var (_, token) = await fixture.CreateUserWithPermissionsAsync("system.users.view");
        fixture.SetBearerToken(token);
        try
        {
            var request = new CreateUserRequest(
                $"unauth-{Guid.NewGuid():N}", "Unauthorized", $"unauth-{Guid.NewGuid():N}@test.local", "Test1234!");
            var response = await Client.PostAsJsonAsync("/api/users", request, IntegrationTestFixture.JsonOptions);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    [Fact]
    public async Task UserWithCreatePermission_CanCreate_Returns200()
    {
        var (_, token) = await fixture.CreateUserWithPermissionsAsync("system.users.create");
        fixture.SetBearerToken(token);
        try
        {
            var username = $"auth-create-{Guid.NewGuid():N}";
            var request = new CreateUserRequest(
                username, "Auth Create Test", $"{username}@test.local", "Test1234!");
            var response = await Client.PostAsJsonAsync("/api/users", request, IntegrationTestFixture.JsonOptions);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    [Fact]
    public async Task UserWithNoPermissions_CannotAccessProtectedEndpoint_Returns403()
    {
        // Create user with a role that has no permissions
        var role = await fixture.CreateRoleAsync();
        var password = "Test1234!";
        var user = await fixture.CreateUserAsync(password: password);

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
            var response = await Client.GetAsync("/api/users");
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    [Fact]
    public async Task AdminToken_HasAllPermissions()
    {
        // Admin should be able to access all endpoints
        var usersResponse = await Client.GetAsync("/api/users");
        usersResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var rolesResponse = await Client.GetAsync("/api/roles");
        rolesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var permissionsResponse = await Client.GetAsync("/api/permissions");
        permissionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var workspacesResponse = await Client.GetAsync("/api/workspaces");
        workspacesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UserWithMultiplePermissions_CanAccessOnlyGrantedEndpoints()
    {
        var (_, token) = await fixture.CreateUserWithPermissionsAsync(
            "system.users.view", "system.roles.view");
        fixture.SetBearerToken(token);
        try
        {
            var usersResponse = await Client.GetAsync("/api/users");
            usersResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var rolesResponse = await Client.GetAsync("/api/roles");
            rolesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Should NOT be able to access workspaces
            var wsResponse = await Client.GetAsync("/api/workspaces");
            wsResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }
}
