namespace Auth.IntegrationTests;

[Collection("Integration")]
public sealed class UsersControllerTests(IntegrationTestFixture fixture)
{
    private HttpClient Client => fixture.Client;

    // --- Auth ---

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        fixture.ClearAuth();
        try
        {
            var response = await Client.GetAsync("/api/users");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    [Fact]
    public async Task GetAll_WithoutPermission_Returns403()
    {
        var (_, token) = await fixture.CreateUserWithPermissionsAsync("system.roles.view");
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

    // --- GetAll ---

    [Fact]
    public async Task GetAll_AsAdmin_ReturnsUsers()
    {
        var response = await Client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.Content
            .ReadFromJsonAsync<IReadOnlyCollection<UserDto>>(IntegrationTestFixture.JsonOptions);
        users.Should().NotBeEmpty();
        users.Should().Contain(u => u.Username == "admin");
    }

    // --- GetById ---

    [Fact]
    public async Task GetById_ExistingUser_Returns200()
    {
        var user = await fixture.CreateUserAsync();

        var response = await Client.GetAsync($"/api/users/{user.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UserDto>(IntegrationTestFixture.JsonOptions);
        result!.Id.Should().Be(user.Id);
        result.Username.Should().Be(user.Username);
    }

    [Fact]
    public async Task GetById_NonexistentUser_Returns404()
    {
        var response = await Client.GetAsync($"/api/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Create ---

    [Fact]
    public async Task Create_ValidUser_ReturnsUserDto()
    {
        var username = $"create-test-{Guid.NewGuid():N}";
        var request = new CreateUserRequest(
            Username: username,
            FullName: "Test Create User",
            Email: $"{username}@test.local",
            Password: "Test1234!");

        var response = await Client.PostAsJsonAsync("/api/users", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDto>(IntegrationTestFixture.JsonOptions);
        user!.Username.Should().Be(username);
        user.FullName.Should().Be("Test Create User");
        user.Email.Should().Be($"{username}@test.local");
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Create_DuplicateUsername_RejectsRequest()
    {
        var user = await fixture.CreateUserAsync();
        var request = new CreateUserRequest(
            Username: user.Username,
            FullName: "Duplicate",
            Email: $"dup-{Guid.NewGuid():N}@test.local",
            Password: "Test1234!");

        var response = await Client.PostAsJsonAsync("/api/users", request, IntegrationTestFixture.JsonOptions);

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task Create_DuplicateEmail_RejectsRequest()
    {
        var user = await fixture.CreateUserAsync();
        var request = new CreateUserRequest(
            Username: $"dup-{Guid.NewGuid():N}",
            FullName: "Duplicate Email",
            Email: user.Email,
            Password: "Test1234!");

        var response = await Client.PostAsJsonAsync("/api/users", request, IntegrationTestFixture.JsonOptions);

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task Create_InvalidEmail_Returns400()
    {
        var request = new CreateUserRequest(
            Username: $"invalid-{Guid.NewGuid():N}",
            FullName: "Invalid Email",
            Email: "",
            Password: "Test1234!");

        var response = await Client.PostAsJsonAsync("/api/users", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // --- Update ---

    [Fact]
    public async Task Update_ExistingUser_ReturnsUpdated()
    {
        var user = await fixture.CreateUserAsync();

        var request = new UpdateUserRequest(
            Username: user.Username,
            FullName: "Updated Full Name",
            Email: user.Email,
            Phone: "+1234567890",
            IsActive: true);

        var response = await Client.PutAsJsonAsync($"/api/users/{user.Id}", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<UserDto>(IntegrationTestFixture.JsonOptions);
        updated!.FullName.Should().Be("Updated Full Name");
        updated.Phone.Should().Be("+1234567890");
    }

    [Fact]
    public async Task Update_NonexistentUser_Returns404()
    {
        var request = new UpdateUserRequest(
            Username: "nonexistent",
            FullName: "None",
            Email: "none@test.local",
            Phone: null,
            IsActive: true);

        var response = await Client.PutAsJsonAsync($"/api/users/{Guid.NewGuid()}", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Patch ---

    [Fact]
    public async Task Patch_PartialUpdate_OnlyUpdatesProvidedFields()
    {
        var user = await fixture.CreateUserAsync();

        var request = new PatchUserRequest(
            Username: default,
            FullName: "Patched Name",
            Email: default,
            Phone: default,
            IsActive: default,
            IsInternalAuthEnabled: default,
            TwoFactorEnabled: default,
            TwoFactorChannel: default,
            Locale: default,
            EmailVerified: default,
            PhoneVerified: default,
            PasswordMaxAgeDays: default);

        var response = await Client.PatchAsJsonAsync($"/api/users/{user.Id}", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var patched = await response.Content.ReadFromJsonAsync<UserDto>(IntegrationTestFixture.JsonOptions);
        patched!.FullName.Should().Be("Patched Name");
        patched.Username.Should().Be(user.Username);
    }

    // --- Delete ---

    [Fact]
    public async Task Delete_ExistingUser_Returns204()
    {
        var user = await fixture.CreateUserAsync();

        var response = await Client.DeleteAsync($"/api/users/{user.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await Client.GetAsync($"/api/users/{user.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_NonexistentUser_Returns404()
    {
        var response = await Client.DeleteAsync($"/api/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Reset Password ---

    [Fact]
    public async Task ResetPassword_ValidRequest_Returns204()
    {
        var user = await fixture.CreateUserAsync();

        var request = new AdminResetPasswordRequest("NewPass1234!");
        var response = await Client.PostAsJsonAsync(
            $"/api/users/{user.Id}/reset-password", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // --- Workspaces ---

    [Fact]
    public async Task GetWorkspaces_ExistingUser_ReturnsWorkspaces()
    {
        var user = await fixture.CreateUserAsync();
        var role = await fixture.CreateRoleAsync();

        var wsResponse = await Client.GetAsync("/api/workspaces");
        var workspaces = await wsResponse.Content
            .ReadFromJsonAsync<IReadOnlyCollection<WorkspaceDto>>(IntegrationTestFixture.JsonOptions);
        var systemWs = workspaces!.First(w => w.Code == "system");

        var wsRequest = new SetUserWorkspacesRequest([
            new UserWorkspaceRolesItem(systemWs.Id, [role.Id])
        ]);
        await Client.PutAsJsonAsync($"/api/users/{user.Id}/workspaces", wsRequest, IntegrationTestFixture.JsonOptions);

        var response = await Client.GetAsync($"/api/users/{user.Id}/workspaces");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content
            .ReadFromJsonAsync<IReadOnlyCollection<UserWorkspaceRolesItem>>(IntegrationTestFixture.JsonOptions);
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SetWorkspaces_ValidRequest_Returns204()
    {
        var user = await fixture.CreateUserAsync();
        var role = await fixture.CreateRoleAsync();

        var wsResponse = await Client.GetAsync("/api/workspaces");
        var workspaces = await wsResponse.Content
            .ReadFromJsonAsync<IReadOnlyCollection<WorkspaceDto>>(IntegrationTestFixture.JsonOptions);
        var systemWs = workspaces!.First(w => w.Code == "system");

        var request = new SetUserWorkspacesRequest([
            new UserWorkspaceRolesItem(systemWs.Id, [role.Id])
        ]);

        var response = await Client.PutAsJsonAsync(
            $"/api/users/{user.Id}/workspaces", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // --- Search ---

    [Fact]
    public async Task Search_ReturnsSearchResponse()
    {
        var request = new SearchRequest(null, null);
        var response = await Client.PostAsJsonAsync("/api/users/search", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content
            .ReadFromJsonAsync<SearchResponse<UserDto>>(IntegrationTestFixture.JsonOptions);
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
    }
}
