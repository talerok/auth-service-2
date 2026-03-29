namespace Auth.IntegrationTests;

[Collection("Integration")]
public sealed class WorkspacesControllerTests(IntegrationTestFixture fixture)
{
    private HttpClient Client => fixture.Client;

    // --- Auth ---

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        fixture.ClearAuth();
        try
        {
            var response = await Client.GetAsync("/api/workspaces");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    // --- GetAll ---

    [Fact]
    public async Task GetAll_AsAdmin_ReturnsWorkspaces()
    {
        var response = await Client.GetAsync("/api/workspaces");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var workspaces = await response.Content
            .ReadFromJsonAsync<IReadOnlyCollection<WorkspaceDto>>(IntegrationTestFixture.JsonOptions);
        workspaces.Should().NotBeEmpty();
        workspaces.Should().Contain(w => w.Code == "system");
    }

    // --- GetById ---

    [Fact]
    public async Task GetById_ExistingWorkspace_Returns200()
    {
        var ws = await fixture.CreateWorkspaceAsync();

        var response = await Client.GetAsync($"/api/workspaces/{ws.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WorkspaceDto>(IntegrationTestFixture.JsonOptions);
        result!.Id.Should().Be(ws.Id);
    }

    [Fact]
    public async Task GetById_NonexistentWorkspace_Returns404()
    {
        var response = await Client.GetAsync($"/api/workspaces/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Create ---

    [Fact]
    public async Task Create_ValidWorkspace_ReturnsWorkspaceDto()
    {
        var name = $"ws-create-{Guid.NewGuid():N}";
        var request = new CreateWorkspaceRequest(name, name, $"Test workspace {name}");

        var response = await Client.PostAsJsonAsync("/api/workspaces", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ws = await response.Content.ReadFromJsonAsync<WorkspaceDto>(IntegrationTestFixture.JsonOptions);
        ws!.Name.Should().Be(name);
        ws.Code.Should().Be(name);
        ws.IsSystem.Should().BeFalse();
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsError()
    {
        var ws = await fixture.CreateWorkspaceAsync();
        var request = new CreateWorkspaceRequest($"dup-{Guid.NewGuid():N}", ws.Code, "Duplicate");

        var response = await Client.PostAsJsonAsync("/api/workspaces", request, IntegrationTestFixture.JsonOptions);

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    // --- Update ---

    [Fact]
    public async Task Update_CustomWorkspace_ReturnsUpdated()
    {
        var ws = await fixture.CreateWorkspaceAsync();
        var newName = $"updated-{Guid.NewGuid():N}";
        var request = new UpdateWorkspaceRequest(newName, ws.Code, "Updated description");

        var response = await Client.PutAsJsonAsync($"/api/workspaces/{ws.Id}", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<WorkspaceDto>(IntegrationTestFixture.JsonOptions);
        updated!.Name.Should().Be(newName);
    }

    // --- Patch ---

    [Fact]
    public async Task Patch_CustomWorkspace_Returns200()
    {
        var ws = await fixture.CreateWorkspaceAsync();
        var request = new PatchWorkspaceRequest(Name: "patched-ws", Code: default, Description: default);

        var response = await Client.PatchAsJsonAsync($"/api/workspaces/{ws.Id}", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var patched = await response.Content.ReadFromJsonAsync<WorkspaceDto>(IntegrationTestFixture.JsonOptions);
        patched!.Name.Should().Be("patched-ws");
    }

    // --- Delete ---

    [Fact]
    public async Task Delete_CustomWorkspace_Returns204()
    {
        var ws = await fixture.CreateWorkspaceAsync();

        var response = await Client.DeleteAsync($"/api/workspaces/{ws.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await Client.GetAsync($"/api/workspaces/{ws.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_SystemWorkspace_ReturnsForbidden()
    {
        var allWs = await Client.GetFromJsonAsync<IReadOnlyCollection<WorkspaceDto>>(
            "/api/workspaces", IntegrationTestFixture.JsonOptions);
        var systemWs = allWs!.First(w => w.Code == "system");

        var response = await Client.DeleteAsync($"/api/workspaces/{systemWs.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_NonexistentWorkspace_Returns404()
    {
        var response = await Client.DeleteAsync($"/api/workspaces/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Search ---

    [Fact]
    public async Task Search_ReturnsSearchResponse()
    {
        var request = new SearchRequest(null, null);
        var response = await Client.PostAsJsonAsync("/api/workspaces/search", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
