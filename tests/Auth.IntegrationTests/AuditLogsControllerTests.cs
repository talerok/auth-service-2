namespace Auth.IntegrationTests;

[Collection("Integration")]
public sealed class AuditLogsControllerTests(IntegrationTestFixture fixture)
{
    private HttpClient Client => fixture.Client;

    [Fact]
    public async Task Search_WithoutToken_Returns401()
    {
        fixture.ClearAuth();
        try
        {
            var response = await Client.PostAsJsonAsync("/api/audit-logs/search",
                new { query = (string?)null, page = 1, pageSize = 20 });
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    [Fact]
    public async Task Search_WithoutPermission_Returns403()
    {
        var (_, token) = await fixture.CreateUserWithPermissionsAsync("system.users.view");
        fixture.SetBearerToken(token);
        try
        {
            var response = await Client.PostAsJsonAsync("/api/audit-logs/search",
                new { query = (string?)null, page = 1, pageSize = 20 });
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    [Fact]
    public async Task Search_AsAdmin_Returns200()
    {
        var response = await Client.PostAsJsonAsync("/api/audit-logs/search",
            new { query = (string?)null, page = 1, pageSize = 20 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
