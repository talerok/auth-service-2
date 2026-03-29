namespace Auth.IntegrationTests;

[Collection("Integration")]
public sealed class RolesControllerTests(IntegrationTestFixture fixture)
{
    private HttpClient Client => fixture.Client;

    // --- Auth ---

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        fixture.ClearAuth();
        try
        {
            var response = await Client.GetAsync("/api/roles");
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
        var (_, token) = await fixture.CreateUserWithPermissionsAsync("system.users.view");
        fixture.SetBearerToken(token);
        try
        {
            var response = await Client.GetAsync("/api/roles");
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    // --- GetAll ---

    [Fact]
    public async Task GetAll_AsAdmin_ReturnsRoles()
    {
        var response = await Client.GetAsync("/api/roles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var roles = await response.Content
            .ReadFromJsonAsync<IReadOnlyCollection<RoleDto>>(IntegrationTestFixture.JsonOptions);
        roles.Should().NotBeEmpty();
        roles.Should().Contain(r => r.Name == "admin");
    }

    // --- GetById ---

    [Fact]
    public async Task GetById_ExistingRole_Returns200()
    {
        var role = await fixture.CreateRoleAsync();

        var response = await Client.GetAsync($"/api/roles/{role.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RoleDto>(IntegrationTestFixture.JsonOptions);
        result!.Id.Should().Be(role.Id);
    }

    [Fact]
    public async Task GetById_NonexistentRole_Returns404()
    {
        var response = await Client.GetAsync($"/api/roles/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Create ---

    [Fact]
    public async Task Create_ValidRole_ReturnsRoleDto()
    {
        var name = $"role-create-{Guid.NewGuid():N}";
        var request = new CreateRoleRequest(name, name, $"Test role {name}");

        var response = await Client.PostAsJsonAsync("/api/roles", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var role = await response.Content.ReadFromJsonAsync<RoleDto>(IntegrationTestFixture.JsonOptions);
        role!.Name.Should().Be(name);
        role.Code.Should().Be(name);
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsError()
    {
        var role = await fixture.CreateRoleAsync();
        var request = new CreateRoleRequest($"dup-{Guid.NewGuid():N}", role.Code, "Duplicate code");

        var response = await Client.PostAsJsonAsync("/api/roles", request, IntegrationTestFixture.JsonOptions);

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    // --- Update ---

    [Fact]
    public async Task Update_ExistingRole_ReturnsUpdated()
    {
        var role = await fixture.CreateRoleAsync();
        var newName = $"updated-{Guid.NewGuid():N}";
        var request = new UpdateRoleRequest(newName, role.Code, "Updated description");

        var response = await Client.PutAsJsonAsync($"/api/roles/{role.Id}", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<RoleDto>(IntegrationTestFixture.JsonOptions);
        updated!.Name.Should().Be(newName);
    }

    // --- Patch ---

    [Fact]
    public async Task Patch_PartialUpdate_Returns200()
    {
        var role = await fixture.CreateRoleAsync();
        var request = new PatchRoleRequest(Name: "patched-name", Code: default, Description: default);

        var response = await Client.PatchAsJsonAsync($"/api/roles/{role.Id}", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var patched = await response.Content.ReadFromJsonAsync<RoleDto>(IntegrationTestFixture.JsonOptions);
        patched!.Name.Should().Be("patched-name");
        patched.Code.Should().Be(role.Code);
    }

    // --- Delete ---

    [Fact]
    public async Task Delete_ExistingRole_Returns204()
    {
        var role = await fixture.CreateRoleAsync();

        var response = await Client.DeleteAsync($"/api/roles/{role.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await Client.GetAsync($"/api/roles/{role.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_NonexistentRole_Returns404()
    {
        var response = await Client.DeleteAsync($"/api/roles/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Permissions ---

    [Fact]
    public async Task GetPermissions_ExistingRole_ReturnsPermissions()
    {
        var role = await fixture.CreateRoleAsync();
        var perm = await fixture.CreatePermissionAsync();
        var setRequest = new SetPermissionsRequest([perm]);
        await Client.PutAsJsonAsync($"/api/roles/{role.Id}/permissions", setRequest, IntegrationTestFixture.JsonOptions);

        var response = await Client.GetAsync($"/api/roles/{role.Id}/permissions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var permissions = await response.Content
            .ReadFromJsonAsync<IReadOnlyCollection<PermissionDto>>(IntegrationTestFixture.JsonOptions);
        permissions.Should().Contain(p => p.Id == perm.Id);
    }

    [Fact]
    public async Task SetPermissions_ValidRequest_Returns204()
    {
        var role = await fixture.CreateRoleAsync();
        var perm = await fixture.CreatePermissionAsync();
        var request = new SetPermissionsRequest([perm]);

        var response = await Client.PutAsJsonAsync(
            $"/api/roles/{role.Id}/permissions", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // --- Search ---

    [Fact]
    public async Task Search_ReturnsSearchResponse()
    {
        var request = new SearchRequest(null, null);
        var response = await Client.PostAsJsonAsync("/api/roles/search", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
