namespace Auth.IntegrationTests;

[Collection("Integration")]
public sealed class ApplicationsControllerTests(IntegrationTestFixture fixture)
{
    private HttpClient Client => fixture.Client;

    // --- Auth ---

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        fixture.ClearAuth();
        try
        {
            var response = await Client.GetAsync("/api/applications");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    // --- GetAll ---

    [Fact]
    public async Task GetAll_AsAdmin_ReturnsApplications()
    {
        var response = await Client.GetAsync("/api/applications");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var apps = await response.Content
            .ReadFromJsonAsync<IReadOnlyCollection<ApplicationDto>>(IntegrationTestFixture.JsonOptions);
        apps.Should().NotBeEmpty();
        apps.Should().Contain(a => a.ClientId == "frontend-app");
    }

    // --- GetById ---

    [Fact]
    public async Task GetById_ExistingApplication_Returns200()
    {
        var created = await fixture.CreateApplicationAsync();

        var response = await Client.GetAsync($"/api/applications/{created.Application.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApplicationDto>(IntegrationTestFixture.JsonOptions);
        result!.Id.Should().Be(created.Application.Id);
    }

    [Fact]
    public async Task GetById_NonexistentApplication_Returns404()
    {
        var response = await Client.GetAsync($"/api/applications/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Create ---

    [Fact]
    public async Task Create_PublicApp_ReturnsApplicationDto()
    {
        var name = $"app-pub-{Guid.NewGuid():N}";
        var request = new CreateApplicationRequest(name, $"Test {name}", IsConfidential: false,
            RedirectUris: ["http://localhost:3000/callback"]);

        var response = await Client.PostAsJsonAsync("/api/applications", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content
            .ReadFromJsonAsync<CreateApplicationResponse>(IntegrationTestFixture.JsonOptions);
        result!.Application.Name.Should().Be(name);
        result.Application.IsConfidential.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ConfidentialApp_ReturnsSecret()
    {
        var name = $"app-conf-{Guid.NewGuid():N}";
        var request = new CreateApplicationRequest(name, $"Test {name}", IsConfidential: true,
            RedirectUris: ["http://localhost:3000/callback"]);

        var response = await Client.PostAsJsonAsync("/api/applications", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content
            .ReadFromJsonAsync<CreateApplicationResponse>(IntegrationTestFixture.JsonOptions);
        result!.Application.IsConfidential.Should().BeTrue();
        result.ClientSecret.Should().NotBeNullOrWhiteSpace();
    }

    // --- Update ---

    [Fact]
    public async Task Update_ExistingApplication_ReturnsUpdated()
    {
        var created = await fixture.CreateApplicationAsync();
        var newName = $"updated-{Guid.NewGuid():N}";
        var request = new UpdateApplicationRequest(
            newName, "Updated", IsActive: true, IsConfidential: false,
            LogoUrl: null, HomepageUrl: null,
            RedirectUris: ["http://localhost:3000/callback"], PostLogoutRedirectUris: [], ConsentType: null);

        var response = await Client.PutAsJsonAsync(
            $"/api/applications/{created.Application.Id}", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ApplicationDto>(IntegrationTestFixture.JsonOptions);
        updated!.Name.Should().Be(newName);
    }

    // --- Patch ---

    [Fact]
    public async Task Patch_ExistingApplication_Returns200()
    {
        var created = await fixture.CreateApplicationAsync();
        var request = new PatchApplicationRequest(
            Name: "patched-app", Description: null, IsActive: null, IsConfidential: null,
            LogoUrl: null, HomepageUrl: null, RedirectUris: null, PostLogoutRedirectUris: null, ConsentType: null);

        var response = await Client.PatchAsJsonAsync(
            $"/api/applications/{created.Application.Id}", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var patched = await response.Content.ReadFromJsonAsync<ApplicationDto>(IntegrationTestFixture.JsonOptions);
        patched!.Name.Should().Be("patched-app");
    }

    // --- Delete ---

    [Fact]
    public async Task Delete_ExistingApplication_Returns204()
    {
        var created = await fixture.CreateApplicationAsync();

        var response = await Client.DeleteAsync($"/api/applications/{created.Application.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await Client.GetAsync($"/api/applications/{created.Application.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_NonexistentApplication_Returns404()
    {
        var response = await Client.DeleteAsync($"/api/applications/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- RegenerateSecret ---

    [Fact]
    public async Task RegenerateSecret_ConfidentialApp_ReturnsNewSecret()
    {
        var created = await fixture.CreateApplicationAsync(isConfidential: true);

        var response = await Client.PostAsync(
            $"/api/applications/{created.Application.Id}/regenerate-secret", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content
            .ReadFromJsonAsync<RegenerateApplicationSecretResponse>(IntegrationTestFixture.JsonOptions);
        result!.ClientSecret.Should().NotBeNullOrWhiteSpace();
        result.ClientSecret.Should().NotBe(created.ClientSecret);
    }

    // --- Search ---

    [Fact]
    public async Task Search_ReturnsSearchResponse()
    {
        var request = new SearchRequest(null, null);
        var response = await Client.PostAsJsonAsync("/api/applications/search", request, IntegrationTestFixture.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
