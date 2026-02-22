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
}
