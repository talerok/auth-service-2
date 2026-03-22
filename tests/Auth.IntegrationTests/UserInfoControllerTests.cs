namespace Auth.IntegrationTests;

[Collection("Integration")]
public sealed class UserInfoControllerTests(IntegrationTestFixture fixture)
{
    private HttpClient Client => fixture.Client;

    [Fact]
    public async Task UserInfo_WithToken_ReturnsSubjectAndClaims()
    {
        var response = await Client.GetAsync("/connect/userinfo");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(IntegrationTestFixture.JsonOptions);
        result.TryGetProperty("sub", out _).Should().BeTrue();
    }

    [Fact]
    public async Task UserInfo_WithoutToken_Returns401()
    {
        fixture.ClearAuth();
        try
        {
            var response = await Client.GetAsync("/connect/userinfo");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            fixture.SetAdminToken();
        }
    }

    [Fact]
    public async Task UserInfo_Post_WorksSameAsGet()
    {
        var response = await Client.PostAsync("/connect/userinfo",
            new FormUrlEncodedContent([]));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(IntegrationTestFixture.JsonOptions);
        result.TryGetProperty("sub", out _).Should().BeTrue();
    }
}
