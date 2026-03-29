namespace Auth.IntegrationTests;

[Collection("Integration")]
public sealed class PermissionsControllerTests(IntegrationTestFixture fixture)
{
    private HttpClient Client => fixture.Client;

    // --- Auth ---

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        fixture.ClearAuth();
        try
        {
            var response = await Client.GetAsync("/api/permissions");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    // --- GetAll ---

    [Fact]
    public async Task GetAll_AsAdmin_ReturnsPermissions()
    {
        var response = await Client.GetAsync("/api/permissions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var permissions = await response.Content
            .ReadFromJsonAsync<IReadOnlyCollection<PermissionDto>>(IntegrationTestFixture.JsonOptions);
        permissions.Should().NotBeEmpty();
        permissions.Should().Contain(p => p.IsSystem);
    }

    // --- GetById ---

    [Fact]
    public async Task GetById_ExistingPermission_Returns200()
    {
        var perm = await fixture.CreatePermissionAsync();

        var response = await Client.GetAsync($"/api/permissions/{perm.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PermissionDto>(IntegrationTestFixture.JsonOptions);
        result!.Id.Should().Be(perm.Id);
    }

    [Fact]
    public async Task GetById_NonexistentPermission_Returns404()
    {
        var response = await Client.GetAsync($"/api/permissions/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Create ---

    [Fact]
    public async Task Create_CustomPermission_ReturnsPermissionDto()
    {
        var code = $"custom.perm.{Guid.NewGuid():N}";
        var request = new CreatePermissionRequest("custom", code, "Test custom permission");

        var response = await Client.PostAsJsonAsync("/api/permissions", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var perm = await response.Content.ReadFromJsonAsync<PermissionDto>(IntegrationTestFixture.JsonOptions);
        perm!.Code.Should().Be(code);
        perm.Domain.Should().Be("custom");
        perm.IsSystem.Should().BeFalse();
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsError()
    {
        var perm = await fixture.CreatePermissionAsync();
        var request = new CreatePermissionRequest(perm.Domain, perm.Code, "Duplicate");

        var response = await Client.PostAsJsonAsync("/api/permissions", request, IntegrationTestFixture.JsonOptions);

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    // --- Update ---

    [Fact]
    public async Task Update_CustomPermission_ReturnsUpdated()
    {
        var perm = await fixture.CreatePermissionAsync();
        var newCode = $"custom.updated.{Guid.NewGuid():N}";
        var request = new UpdatePermissionRequest(newCode, "Updated description");

        var response = await Client.PutAsJsonAsync($"/api/permissions/{perm.Id}", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<PermissionDto>(IntegrationTestFixture.JsonOptions);
        updated!.Code.Should().Be(newCode);
    }

    // --- Patch ---

    [Fact]
    public async Task Patch_CustomPermission_Returns200()
    {
        var perm = await fixture.CreatePermissionAsync();
        var request = new PatchPermissionRequest(Code: default, Description: "Patched description");

        var response = await Client.PatchAsJsonAsync($"/api/permissions/{perm.Id}", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var patched = await response.Content.ReadFromJsonAsync<PermissionDto>(IntegrationTestFixture.JsonOptions);
        patched!.Description.Should().Be("Patched description");
        patched.Code.Should().Be(perm.Code);
    }

    // --- Delete ---

    [Fact]
    public async Task Delete_CustomPermission_Returns204()
    {
        var perm = await fixture.CreatePermissionAsync();

        var response = await Client.DeleteAsync($"/api/permissions/{perm.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await Client.GetAsync($"/api/permissions/{perm.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_SystemPermission_ReturnsForbidden()
    {
        var allPerms = await Client.GetFromJsonAsync<IReadOnlyCollection<PermissionDto>>(
            "/api/permissions", IntegrationTestFixture.JsonOptions);
        var systemPerm = allPerms!.First(p => p.IsSystem);

        var response = await Client.DeleteAsync($"/api/permissions/{systemPerm.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_NonexistentPermission_Returns404()
    {
        var response = await Client.DeleteAsync($"/api/permissions/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Search ---

    [Fact]
    public async Task Search_ReturnsSearchResponse()
    {
        var request = new SearchRequest(null, null);
        var response = await Client.PostAsJsonAsync("/api/permissions/search", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
