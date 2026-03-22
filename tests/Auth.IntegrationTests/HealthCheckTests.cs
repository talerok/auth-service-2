namespace Auth.IntegrationTests;

[Collection("Integration")]
public sealed class HealthCheckTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await fixture.Client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
