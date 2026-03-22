namespace Auth.IntegrationTests;

[Collection("Integration")]
public sealed class ServiceAccountsControllerTests(IntegrationTestFixture fixture)
{
    private HttpClient Client => fixture.Client;

    // --- Auth ---

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        fixture.ClearAuth();
        try
        {
            var response = await Client.GetAsync("/api/service-accounts");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    // --- GetAll ---

    [Fact]
    public async Task GetAll_AsAdmin_ReturnsServiceAccounts()
    {
        await fixture.CreateServiceAccountAsync();

        var response = await Client.GetAsync("/api/service-accounts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var accounts = await response.Content
            .ReadFromJsonAsync<IReadOnlyCollection<ServiceAccountDto>>(IntegrationTestFixture.JsonOptions);
        accounts.Should().NotBeEmpty();
    }

    // --- GetById ---

    [Fact]
    public async Task GetById_ExistingServiceAccount_Returns200()
    {
        var created = await fixture.CreateServiceAccountAsync();

        var response = await Client.GetAsync($"/api/service-accounts/{created.ServiceAccount.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content
            .ReadFromJsonAsync<ServiceAccountDto>(IntegrationTestFixture.JsonOptions);
        result!.Id.Should().Be(created.ServiceAccount.Id);
    }

    [Fact]
    public async Task GetById_NonexistentServiceAccount_Returns404()
    {
        var response = await Client.GetAsync($"/api/service-accounts/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Create ---

    [Fact]
    public async Task Create_ValidServiceAccount_ReturnsWithSecret()
    {
        var name = $"sa-create-{Guid.NewGuid():N}";
        var request = new CreateServiceAccountRequest(name, $"Test SA {name}");

        var response = await Client.PostAsJsonAsync("/api/service-accounts", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content
            .ReadFromJsonAsync<CreateServiceAccountResponse>(IntegrationTestFixture.JsonOptions);
        result!.ServiceAccount.Name.Should().Be(name);
        result.ClientSecret.Should().NotBeNullOrWhiteSpace();
    }

    // --- Update ---

    [Fact]
    public async Task Update_ExistingServiceAccount_ReturnsUpdated()
    {
        var created = await fixture.CreateServiceAccountAsync();
        var newName = $"sa-updated-{Guid.NewGuid():N}";
        var request = new UpdateServiceAccountRequest(newName, "Updated description", IsActive: true);

        var response = await Client.PutAsJsonAsync(
            $"/api/service-accounts/{created.ServiceAccount.Id}", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ServiceAccountDto>(IntegrationTestFixture.JsonOptions);
        updated!.Name.Should().Be(newName);
    }

    // --- Patch ---

    [Fact]
    public async Task Patch_ExistingServiceAccount_Returns200()
    {
        var created = await fixture.CreateServiceAccountAsync();
        var request = new PatchServiceAccountRequest(Name: "patched-sa", Description: null, IsActive: null);

        var response = await Client.PatchAsJsonAsync(
            $"/api/service-accounts/{created.ServiceAccount.Id}", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var patched = await response.Content.ReadFromJsonAsync<ServiceAccountDto>(IntegrationTestFixture.JsonOptions);
        patched!.Name.Should().Be("patched-sa");
    }

    // --- Delete ---

    [Fact]
    public async Task Delete_ExistingServiceAccount_Returns204()
    {
        var created = await fixture.CreateServiceAccountAsync();

        var response = await Client.DeleteAsync($"/api/service-accounts/{created.ServiceAccount.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await Client.GetAsync($"/api/service-accounts/{created.ServiceAccount.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_NonexistentServiceAccount_Returns404()
    {
        var response = await Client.DeleteAsync($"/api/service-accounts/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Workspaces ---

    [Fact]
    public async Task GetWorkspaces_ExistingServiceAccount_ReturnsWorkspaces()
    {
        var created = await fixture.CreateServiceAccountAsync();
        var role = await fixture.CreateRoleAsync();

        var allWs = await Client.GetFromJsonAsync<IReadOnlyCollection<WorkspaceDto>>(
            "/api/workspaces", IntegrationTestFixture.JsonOptions);
        var systemWs = allWs!.First(w => w.Code == "system");

        var wsRequest = new SetServiceAccountWorkspacesRequest([
            new ServiceAccountWorkspaceRolesItem(systemWs.Id, [role.Id])
        ]);
        await Client.PutAsJsonAsync(
            $"/api/service-accounts/{created.ServiceAccount.Id}/workspaces",
            wsRequest, IntegrationTestFixture.JsonOptions);

        var response = await Client.GetAsync($"/api/service-accounts/{created.ServiceAccount.Id}/workspaces");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SetWorkspaces_ValidRequest_Returns204()
    {
        var created = await fixture.CreateServiceAccountAsync();
        var role = await fixture.CreateRoleAsync();

        var allWs = await Client.GetFromJsonAsync<IReadOnlyCollection<WorkspaceDto>>(
            "/api/workspaces", IntegrationTestFixture.JsonOptions);
        var systemWs = allWs!.First(w => w.Code == "system");

        var request = new SetServiceAccountWorkspacesRequest([
            new ServiceAccountWorkspaceRolesItem(systemWs.Id, [role.Id])
        ]);

        var response = await Client.PutAsJsonAsync(
            $"/api/service-accounts/{created.ServiceAccount.Id}/workspaces",
            request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // --- RegenerateSecret ---

    [Fact]
    public async Task RegenerateSecret_ExistingServiceAccount_ReturnsNewSecret()
    {
        var created = await fixture.CreateServiceAccountAsync();

        var response = await Client.PostAsync(
            $"/api/service-accounts/{created.ServiceAccount.Id}/regenerate-secret", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content
            .ReadFromJsonAsync<RegenerateServiceAccountSecretResponse>(IntegrationTestFixture.JsonOptions);
        result!.ClientSecret.Should().NotBeNullOrWhiteSpace();
        result.ClientSecret.Should().NotBe(created.ClientSecret);
    }

    // --- Search ---

    [Fact]
    public async Task Search_ReturnsSearchResponse()
    {
        var request = new SearchRequest(null, null);
        var response = await Client.PostAsJsonAsync("/api/service-accounts/search", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
