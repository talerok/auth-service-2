namespace Auth.IntegrationTests;

[Collection("Integration")]
public sealed class SearchControllerTests(IntegrationTestFixture fixture)
{
    private HttpClient Client => fixture.Client;

    [Fact]
    public async Task ReindexAll_AsAdmin_Returns204()
    {
        var response = await Client.PostAsync("/api/search/reindex", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ReindexUsers_AsAdmin_Returns204()
    {
        var response = await Client.PostAsync("/api/search/reindex/users", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Reindex_WithoutPermission_Returns403()
    {
        var (_, token) = await fixture.CreateUserWithPermissionsAsync("system.users.view");
        fixture.SetBearerToken(token);
        try
        {
            var response = await Client.PostAsync("/api/search/reindex", null);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }
}
