using System.Net;
using FluentAssertions;

namespace Auth.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class UsersControllerIntegrationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task GetAll_WithoutAccessToken_ReturnsUnauthorized()
    {
        fixture.ClearAuth();

        var response = await fixture.Client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithUserWithoutPermissions_ReturnsForbidden()
    {
        fixture.ClearAuth();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"user_{suffix}";

        var registerResponse = await fixture.Client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            email = $"{username}@example.com",
            password = "password123"
        });
        registerResponse.EnsureSuccessStatusCode();

        var userTokens = await fixture.LoginAsync(username, "password123");
        fixture.SetBearerToken(userTokens.AccessToken);

        var response = await fixture.Client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithAdminToken_ReturnsCreatedUser()
    {
        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"created_{suffix}";

        var response = await fixture.Client.PostAsJsonAsync("/api/users", new
        {
            username,
            email = $"{username}@example.com",
            password = "strong-password",
            isActive = true
        });

        response.IsSuccessStatusCode.Should().BeTrue();
        var createdUser = await response.Content.ReadFromJsonAsync<UserDto>();
        createdUser.Should().NotBeNull();
        createdUser!.Id.Should().NotBe(Guid.Empty);
        createdUser.Username.Should().Be(username);
        createdUser.Email.Should().Be($"{username}@example.com");
    }

    [Fact]
    public async Task GetWorkspaces_WhenUserDoesNotExist_ReturnsNotFound()
    {
        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);

        var response = await fixture.Client.GetAsync($"/api/users/{Guid.NewGuid()}/workspaces");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetWorkspaces_WhenUserHasNoWorkspaces_ReturnsEmptyArray()
    {
        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var createUserResponse = await fixture.Client.PostAsJsonAsync("/api/users", new
        {
            username = $"user_{suffix}",
            email = $"user_{suffix}@example.com",
            password = "strong-password",
            isActive = true
        });
        createUserResponse.EnsureSuccessStatusCode();
        var user = await createUserResponse.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();

        var response = await fixture.Client.GetAsync($"/api/users/{user!.Id}/workspaces");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<UserWorkspaceRolesItem>>();
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWorkspaces_AfterSettingWorkspaces_ReturnsWorkspacesWithRoleIds()
    {
        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var createUserResponse = await fixture.Client.PostAsJsonAsync("/api/users", new
        {
            username = $"user_{suffix}",
            email = $"user_{suffix}@example.com",
            password = "strong-password",
            isActive = true
        });
        createUserResponse.EnsureSuccessStatusCode();
        var user = await createUserResponse.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();

        var workspacesResponse = await fixture.Client.GetAsync("/api/workspaces");
        workspacesResponse.EnsureSuccessStatusCode();
        var workspaces = await workspacesResponse.Content.ReadFromJsonAsync<List<WorkspaceDto>>();
        workspaces.Should().NotBeNullOrEmpty();
        var workspace = workspaces!.First();

        var rolesResponse = await fixture.Client.GetAsync("/api/roles");
        rolesResponse.EnsureSuccessStatusCode();
        var roles = await rolesResponse.Content.ReadFromJsonAsync<List<RoleDto>>();
        roles.Should().NotBeNullOrEmpty();
        var role = roles!.First();

        await fixture.Client.PutAsJsonAsync($"/api/users/{user!.Id}/workspaces", new
        {
            workspaces = new[]
            {
                new { workSpaceId = workspace.Id, roleIds = new[] { role.Id } }
            }
        });

        var response = await fixture.Client.GetAsync($"/api/users/{user.Id}/workspaces");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<UserWorkspaceRolesItem>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result!.Single().WorkSpaceId.Should().Be(workspace.Id);
        result.Single().RoleIds.Should().ContainSingle(id => id == role.Id);
    }
}
