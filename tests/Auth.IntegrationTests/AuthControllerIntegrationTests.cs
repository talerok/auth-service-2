using Auth.Application;
using Auth.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Auth.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class AuthControllerIntegrationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Login_WhenCredentialsAreValid_ReturnsTokens()
    {
        fixture.ClearAuth();

        var response = await fixture.Client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "admin",
            password = "admin"
        });

        response.IsSuccessStatusCode.Should().BeTrue();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(IntegrationTestFixture.JsonOptions);
        login.Should().NotBeNull();
        login!.RequiresTwoFactor.Should().BeFalse();
        login.Tokens.Should().NotBeNull();
        login.Tokens!.AccessToken.Should().NotBeNullOrWhiteSpace();
        login.Tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WhenCredentialsAreInvalid_ReturnsUnauthorizedProblem()
    {
        fixture.ClearAuth();

        var response = await fixture.Client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "admin",
            password = "wrong-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task Refresh_WhenTokenIsValid_ReturnsNewTokens()
    {
        fixture.ClearAuth();
        var login = await fixture.LoginAsync("admin", "admin");

        var response = await fixture.Client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = login.RefreshToken
        });

        response.IsSuccessStatusCode.Should().BeTrue();
        var refreshed = await response.Content.ReadFromJsonAsync<AuthTokensResponse>(IntegrationTestFixture.JsonOptions);
        refreshed.Should().NotBeNull();
        refreshed!.RefreshToken.Should().NotBe(login.RefreshToken);
    }

    [Fact]
    public async Task Revoke_WithoutAccessToken_ReturnsRfc7807Unauthorized()
    {
        fixture.ClearAuth();

        var response = await fixture.Client.PostAsJsonAsync("/api/auth/revoke", new
        {
            refreshToken = "fake-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Unauthorized");
        problem.Detail.Should().Be("Authentication is required");
        problem.Extensions.Should().ContainKey("code");
        problem.Extensions["code"]?.ToString().Should().Be(AuthErrorCatalog.AuthenticationRequired);
    }

    [Fact]
    public async Task TwoFactor_EnableConfirmDisable_LoginFlow_WorksEndToEnd()
    {
        fixture.ClearAuth();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"user_{suffix}";
        var email = $"{username}@example.com";
        var password = "password123";

        var registerResponse = await fixture.Client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            email,
            password
        });
        registerResponse.EnsureSuccessStatusCode();

        var initialLogin = await fixture.LoginAsync(username, password);
        fixture.SetBearerToken(initialLogin.AccessToken);

        var startEnableResponse = await fixture.Client.PostAsJsonAsync("/api/auth/2fa/enable", new
        {
            channel = "email",
            isHighRisk = false
        });
        startEnableResponse.IsSuccessStatusCode.Should().BeTrue();
        var activationChallenge = await startEnableResponse.Content.ReadFromJsonAsync<EnableTwoFactorResponse>(IntegrationTestFixture.JsonOptions);
        activationChallenge.Should().NotBeNull();

        await fixture.WaitForChallengeDeliveryAsync(activationChallenge.ChallengeId, TimeSpan.FromSeconds(3));

        var confirmResponse = await fixture.Client.PostAsJsonAsync("/api/auth/2fa/confirm", new
        {
            challengeId = activationChallenge.ChallengeId,
            channel = "email",
            otp = "123456"
        });
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        fixture.ClearAuth();
        var loginResponse = await fixture.Client.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password
        });
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(IntegrationTestFixture.JsonOptions);
        login.Should().NotBeNull();
        login!.RequiresTwoFactor.Should().BeTrue();
        login.Tokens.Should().BeNull();
        login.ChallengeId.Should().NotBeNull();

        await fixture.WaitForChallengeDeliveryAsync(login.ChallengeId!.Value, TimeSpan.FromSeconds(3));

        var verifyResponse = await fixture.Client.PostAsJsonAsync("/api/auth/2fa/login/verify", new
        {
            challengeId = login.ChallengeId,
            channel = "email",
            otp = "123456"
        });
        verifyResponse.EnsureSuccessStatusCode();
        var verifiedTokens = await verifyResponse.Content.ReadFromJsonAsync<AuthTokensResponse>(IntegrationTestFixture.JsonOptions);
        verifiedTokens.Should().NotBeNull();
        verifiedTokens!.AccessToken.Should().NotBeNullOrWhiteSpace();

        fixture.SetBearerToken(verifiedTokens.AccessToken);
        var disableResponse = await fixture.Client.PostAsync("/api/auth/2fa/disable", null);
        disableResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        fixture.ClearAuth();
        var postDisableLoginResponse = await fixture.Client.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password
        });
        postDisableLoginResponse.EnsureSuccessStatusCode();
        var postDisableLogin = await postDisableLoginResponse.Content.ReadFromJsonAsync<LoginResponse>(IntegrationTestFixture.JsonOptions);
        postDisableLogin.Should().NotBeNull();
        postDisableLogin!.RequiresTwoFactor.Should().BeFalse();
        postDisableLogin.Tokens.Should().NotBeNull();
    }

    [Fact]
    public async Task TwoFactor_EnableWithUnsupportedChannel_ReturnsValidationError()
    {
        fixture.ClearAuth();
        var login = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(login.AccessToken);

        var startEnableResponse = await fixture.Client.PostAsJsonAsync("/api/auth/2fa/enable", new
        {
            channel = "totp"
        });

        startEnableResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await startEnableResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("One or more validation errors occurred.");
    }

    [Fact]
    public async Task Login_WhenMustChangePassword_Returns200WithRequiresPasswordChangeTrue()
    {
        fixture.ClearAuth();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"mcp_{suffix}";
        const string password = "password123";

        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);
        var createResponse = await fixture.Client.PostAsJsonAsync("/api/users", new
        {
            username,
            email = $"{username}@example.com",
            password,
            isActive = true,
            mustChangePassword = true
        });
        createResponse.EnsureSuccessStatusCode();

        fixture.ClearAuth();
        var response = await fixture.Client.PostAsJsonAsync("/api/auth/login", new { username, password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(IntegrationTestFixture.JsonOptions);
        login.Should().NotBeNull();
        login!.RequiresPasswordChange.Should().BeTrue();
    }

    [Fact]
    public async Task Login_WhenMustChangePassword_DoesNotReturnAccessToken()
    {
        fixture.ClearAuth();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"mcp_{suffix}";
        const string password = "password123";

        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);
        await fixture.Client.PostAsJsonAsync("/api/users", new
        {
            username,
            email = $"{username}@example.com",
            password,
            isActive = true,
            mustChangePassword = true
        });

        fixture.ClearAuth();
        var response = await fixture.Client.PostAsJsonAsync("/api/auth/login", new { username, password });
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(IntegrationTestFixture.JsonOptions);

        login.Should().NotBeNull();
        login!.Tokens.Should().BeNull();
    }

    [Fact]
    public async Task ForcedChangePassword_WithValidChallenge_Returns200WithTokens()
    {
        fixture.ClearAuth();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"mcp_{suffix}";
        const string password = "password123";

        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);
        await fixture.Client.PostAsJsonAsync("/api/users", new
        {
            username,
            email = $"{username}@example.com",
            password,
            isActive = true,
            mustChangePassword = true
        });

        fixture.ClearAuth();
        var loginResponse = await fixture.Client.PostAsJsonAsync("/api/auth/login", new { username, password });
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(IntegrationTestFixture.JsonOptions);
        login.Should().NotBeNull();
        login!.PasswordChangeChallengeId.Should().NotBeNull();

        var changeResponse = await fixture.Client.PostAsJsonAsync("/api/auth/password/forced-change", new
        {
            challengeId = login.PasswordChangeChallengeId,
            newPassword = "NewSecurePassword1!"
        });

        changeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await changeResponse.Content.ReadFromJsonAsync<AuthTokensResponse>(IntegrationTestFixture.JsonOptions);
        tokens.Should().NotBeNull();
        tokens!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ForcedChangePassword_ThenLoginWithNewPassword_ReturnsTokens()
    {
        fixture.ClearAuth();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"mcp_{suffix}";
        const string originalPassword = "password123";
        const string newPassword = "NewSecurePassword1!";

        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);
        await fixture.Client.PostAsJsonAsync("/api/users", new
        {
            username,
            email = $"{username}@example.com",
            password = originalPassword,
            isActive = true,
            mustChangePassword = true
        });

        fixture.ClearAuth();
        var loginResponse = await fixture.Client.PostAsJsonAsync("/api/auth/login", new { username, password = originalPassword });
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(IntegrationTestFixture.JsonOptions);

        await fixture.Client.PostAsJsonAsync("/api/auth/password/forced-change", new
        {
            challengeId = login!.PasswordChangeChallengeId,
            newPassword
        });

        var finalLoginResponse = await fixture.Client.PostAsJsonAsync("/api/auth/login", new { username, password = newPassword });
        finalLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var finalLogin = await finalLoginResponse.Content.ReadFromJsonAsync<LoginResponse>(IntegrationTestFixture.JsonOptions);
        finalLogin.Should().NotBeNull();
        finalLogin!.Tokens.Should().NotBeNull();
        finalLogin.Tokens!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ForcedChangePassword_WithExpiredChallenge_Returns401()
    {
        fixture.ClearAuth();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"mcp_{suffix}";
        const string password = "password123";

        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);
        var createResponse = await fixture.Client.PostAsJsonAsync("/api/users", new
        {
            username,
            email = $"{username}@example.com",
            password,
            isActive = true
        });
        createResponse.EnsureSuccessStatusCode();
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>(IntegrationTestFixture.JsonOptions);

        var expiredChallengeId = Guid.Empty;
        await fixture.ExecuteAsync(async dbContext =>
        {
            var user = await dbContext.Users.FirstAsync(x => x.Id == createdUser!.Id);
            user.MarkMustChangePassword();
            var challenge = PasswordChangeChallenge.Create(user.Id, DateTime.UtcNow.AddMinutes(1));
            dbContext.PasswordChangeChallenges.Add(challenge);
            await dbContext.SaveChangesAsync();
            expiredChallengeId = challenge.Id;
            dbContext.Entry(challenge).Property("ExpiresAt").CurrentValue = DateTime.UtcNow.AddMinutes(-5);
            await dbContext.SaveChangesAsync();
        });

        fixture.ClearAuth();
        var changeResponse = await fixture.Client.PostAsJsonAsync("/api/auth/password/forced-change", new
        {
            challengeId = expiredChallengeId,
            newPassword = "NewSecurePassword1!"
        });

        changeResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var problem = await changeResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Extensions["code"]?.ToString().Should().Be(AuthErrorCatalog.InvalidPasswordChangeChallenge);
    }

    [Fact]
    public async Task ForcedChangePassword_WithUsedChallenge_Returns401()
    {
        fixture.ClearAuth();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"mcp_{suffix}";
        const string password = "password123";

        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);
        await fixture.Client.PostAsJsonAsync("/api/users", new
        {
            username,
            email = $"{username}@example.com",
            password,
            isActive = true,
            mustChangePassword = true
        });

        fixture.ClearAuth();
        var loginResponse = await fixture.Client.PostAsJsonAsync("/api/auth/login", new { username, password });
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(IntegrationTestFixture.JsonOptions);

        await fixture.Client.PostAsJsonAsync("/api/auth/password/forced-change", new
        {
            challengeId = login!.PasswordChangeChallengeId,
            newPassword = "NewSecurePassword1!"
        });

        var secondChangeResponse = await fixture.Client.PostAsJsonAsync("/api/auth/password/forced-change", new
        {
            challengeId = login.PasswordChangeChallengeId,
            newPassword = "AnotherPassword1!"
        });

        secondChangeResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ForcedChangePassword_WithRandomGuid_Returns401()
    {
        fixture.ClearAuth();

        var response = await fixture.Client.PostAsJsonAsync("/api/auth/password/forced-change", new
        {
            challengeId = Guid.NewGuid(),
            newPassword = "NewSecurePassword1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

}
