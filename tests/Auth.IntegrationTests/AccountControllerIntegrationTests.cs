using Auth.Application;
using Auth.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Auth.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class AccountControllerIntegrationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task TokenEndpoint_PasswordGrant_WhenCredentialsAreValid_ReturnsTokens()
    {
        fixture.ClearAuth();

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = "admin",
            ["password"] = "admin",
            ["client_id"] = "mobile-app",
            ["scope"] = "openid profile email ws"
        });

        var response = await fixture.Client.PostAsync("/connect/token", content);

        response.IsSuccessStatusCode.Should().BeTrue();
        var tokens = await response.Content.ReadFromJsonAsync<OidcTokenResponse>(IntegrationTestFixture.JsonOptions);
        tokens.Should().NotBeNull();
        tokens!.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();
        tokens.TokenType.Should().Be("Bearer");
        tokens.ExpiresIn.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task TokenEndpoint_PasswordGrant_WhenCredentialsAreInvalid_Returns400()
    {
        fixture.ClearAuth();

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = "admin",
            ["password"] = "wrong-password",
            ["client_id"] = "mobile-app",
            ["scope"] = "openid profile email ws"
        });

        var response = await fixture.Client.PostAsync("/connect/token", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<OidcErrorResponse>(IntegrationTestFixture.JsonOptions);
        error.Should().NotBeNull();
        error!.Error.Should().Be("invalid_grant");
    }

    [Fact]
    public async Task TokenEndpoint_RefreshGrant_WhenTokenIsValid_ReturnsNewTokens()
    {
        fixture.ClearAuth();
        var login = await fixture.LoginAsync("admin", "admin");

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = login.RefreshToken!,
            ["client_id"] = "mobile-app"
        });

        var response = await fixture.Client.PostAsync("/connect/token", content);

        response.IsSuccessStatusCode.Should().BeTrue();
        var refreshed = await response.Content.ReadFromJsonAsync<OidcTokenResponse>(IntegrationTestFixture.JsonOptions);
        refreshed.Should().NotBeNull();
        refreshed!.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshed.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ApiEndpoint_WithoutAccessToken_Returns401()
    {
        fixture.ClearAuth();

        var response = await fixture.Client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TwoFactor_EnableConfirmDisable_LoginFlow_WorksEndToEnd()
    {
        fixture.ClearAuth();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"user_{suffix}";
        var email = $"{username}@example.com";
        var password = "password123";

        var registerResponse = await fixture.Client.PostAsJsonAsync("/api/account/register", new
        {
            username,
            fullName = username,
            email,
            password
        });
        registerResponse.EnsureSuccessStatusCode();

        var initialLogin = await fixture.LoginAsync(username, password);
        fixture.SetBearerToken(initialLogin.AccessToken);

        var startEnableResponse = await fixture.Client.PostAsJsonAsync("/api/account/2fa/enable", new
        {
            channel = "email",
            isHighRisk = false
        });
        startEnableResponse.IsSuccessStatusCode.Should().BeTrue();
        var activationChallenge = await startEnableResponse.Content.ReadFromJsonAsync<EnableTwoFactorResponse>(IntegrationTestFixture.JsonOptions);
        activationChallenge.Should().NotBeNull();

        await fixture.WaitForChallengeDeliveryAsync(activationChallenge!.ChallengeId, TimeSpan.FromSeconds(3));

        var confirmResponse = await fixture.Client.PostAsJsonAsync("/api/account/2fa/confirm", new
        {
            challengeId = activationChallenge.ChallengeId,
            channel = "email",
            otp = "123456"
        });
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Login with 2FA enabled — should get mfa_required
        fixture.ClearAuth();
        var mfaLoginContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = username,
            ["password"] = password,
            ["client_id"] = "mobile-app",
            ["scope"] = "openid profile email ws"
        });
        var mfaLoginResponse = await fixture.Client.PostAsync("/connect/token", mfaLoginContent);
        mfaLoginResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var mfaError = await mfaLoginResponse.Content.ReadFromJsonAsync<OidcErrorResponse>(IntegrationTestFixture.JsonOptions);
        mfaError.Should().NotBeNull();
        mfaError!.Error.Should().Be("mfa_required");
        mfaError.MfaToken.Should().NotBeNullOrWhiteSpace();
        mfaError.MfaChannel.Should().Be("email");

        await fixture.WaitForChallengeDeliveryAsync(Guid.Parse(mfaError.MfaToken!), TimeSpan.FromSeconds(3));

        // Complete MFA with OTP
        var mfaVerifyContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "urn:custom:mfa_otp",
            ["mfa_token"] = mfaError.MfaToken!,
            ["otp"] = "123456",
            ["mfa_channel"] = "email",
            ["client_id"] = "mobile-app",
            ["scope"] = "openid profile email ws"
        });
        var mfaVerifyResponse = await fixture.Client.PostAsync("/connect/token", mfaVerifyContent);
        mfaVerifyResponse.EnsureSuccessStatusCode();
        var verifiedTokens = await mfaVerifyResponse.Content.ReadFromJsonAsync<OidcTokenResponse>(IntegrationTestFixture.JsonOptions);
        verifiedTokens.Should().NotBeNull();
        verifiedTokens!.AccessToken.Should().NotBeNullOrWhiteSpace();

        // Disable 2FA
        fixture.SetBearerToken(verifiedTokens.AccessToken);
        var disableResponse = await fixture.Client.PostAsync("/api/account/2fa/disable", null);
        disableResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Login after disabling 2FA — should get tokens directly
        fixture.ClearAuth();
        var postDisableLogin = await fixture.LoginAsync(username, password);
        postDisableLogin.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task TwoFactor_SmsChannel_EnableConfirmLogin_WorksEndToEnd()
    {
        fixture.ClearAuth();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"sms_{suffix}";
        var email = $"{username}@example.com";
        var password = "password123";
        var phone = "+71234567890";

        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);
        var createResponse = await fixture.Client.PostAsJsonAsync("/api/users", new
        {
            username,
            fullName = username,
            email,
            password,
            phone,
            isActive = true
        });
        createResponse.EnsureSuccessStatusCode();

        fixture.ClearAuth();
        var tokens = await fixture.LoginAsync(username, password);
        fixture.SetBearerToken(tokens.AccessToken);

        var startEnableResponse = await fixture.Client.PostAsJsonAsync("/api/account/2fa/enable", new
        {
            channel = "sms",
            isHighRisk = false
        });
        startEnableResponse.IsSuccessStatusCode.Should().BeTrue();
        var activationChallenge = await startEnableResponse.Content.ReadFromJsonAsync<EnableTwoFactorResponse>(IntegrationTestFixture.JsonOptions);
        activationChallenge.Should().NotBeNull();
        activationChallenge!.Channel.Should().Be(TwoFactorChannel.Sms);

        await fixture.WaitForChallengeDeliveryAsync(activationChallenge.ChallengeId, TimeSpan.FromSeconds(3));

        var confirmResponse = await fixture.Client.PostAsJsonAsync("/api/account/2fa/confirm", new
        {
            challengeId = activationChallenge.ChallengeId,
            channel = "sms",
            otp = "123456"
        });
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Login with SMS 2FA enabled
        fixture.ClearAuth();
        var mfaLoginContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = username,
            ["password"] = password,
            ["client_id"] = "mobile-app",
            ["scope"] = "openid profile email ws"
        });
        var mfaLoginResponse = await fixture.Client.PostAsync("/connect/token", mfaLoginContent);
        mfaLoginResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var mfaError = await mfaLoginResponse.Content.ReadFromJsonAsync<OidcErrorResponse>(IntegrationTestFixture.JsonOptions);
        mfaError.Should().NotBeNull();
        mfaError!.Error.Should().Be("mfa_required");
        mfaError.MfaChannel.Should().Be("sms");

        await fixture.WaitForChallengeDeliveryAsync(Guid.Parse(mfaError.MfaToken!), TimeSpan.FromSeconds(3));

        var mfaVerifyContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "urn:custom:mfa_otp",
            ["mfa_token"] = mfaError.MfaToken!,
            ["otp"] = "123456",
            ["mfa_channel"] = "sms",
            ["client_id"] = "mobile-app",
            ["scope"] = "openid profile email ws"
        });
        var mfaVerifyResponse = await fixture.Client.PostAsync("/connect/token", mfaVerifyContent);
        mfaVerifyResponse.EnsureSuccessStatusCode();
        var verifiedTokens = await mfaVerifyResponse.Content.ReadFromJsonAsync<OidcTokenResponse>(IntegrationTestFixture.JsonOptions);
        verifiedTokens.Should().NotBeNull();
        verifiedTokens!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task TwoFactor_SmsChannel_WhenUserHasNoPhone_Returns400PhoneRequired()
    {
        fixture.ClearAuth();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"nophone_{suffix}";
        var email = $"{username}@example.com";
        var password = "password123";

        var registerResponse = await fixture.Client.PostAsJsonAsync("/api/account/register", new
        {
            username,
            fullName = username,
            email,
            password
        });
        registerResponse.EnsureSuccessStatusCode();

        var tokens = await fixture.LoginAsync(username, password);
        fixture.SetBearerToken(tokens.AccessToken);

        var startEnableResponse = await fixture.Client.PostAsJsonAsync("/api/account/2fa/enable", new
        {
            channel = "sms",
            isHighRisk = false
        });

        startEnableResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await startEnableResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Extensions["code"]?.ToString().Should().Be(TwoFactorErrorCatalog.PhoneRequired);
    }

    [Fact]
    public async Task TwoFactor_EnableWithUnsupportedChannel_ReturnsValidationError()
    {
        fixture.ClearAuth();
        var login = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(login.AccessToken);

        var startEnableResponse = await fixture.Client.PostAsJsonAsync("/api/account/2fa/enable", new
        {
            channel = "totp"
        });

        startEnableResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await startEnableResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("One or more validation errors occurred.");
    }

    [Fact]
    public async Task TokenEndpoint_PasswordGrant_WhenMustChangePassword_Returns400WithChallengeId()
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
            fullName = username,
            email = $"{username}@example.com",
            password,
            isActive = true,
            mustChangePassword = true
        });
        createResponse.EnsureSuccessStatusCode();

        fixture.ClearAuth();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = username,
            ["password"] = password,
            ["client_id"] = "mobile-app",
            ["scope"] = "openid profile email ws"
        });
        var response = await fixture.Client.PostAsync("/connect/token", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<OidcErrorResponse>(IntegrationTestFixture.JsonOptions);
        error.Should().NotBeNull();
        error!.Error.Should().Be("password_change_required");
        error.ChallengeId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ForcedChangePassword_WithValidChallenge_ThenLoginSucceeds()
    {
        fixture.ClearAuth();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"mcp_{suffix}";
        const string password = "password123";
        const string newPassword = "NewSecurePassword1!";

        var admin = await fixture.LoginAsync("admin", "admin");
        fixture.SetBearerToken(admin.AccessToken);
        await fixture.Client.PostAsJsonAsync("/api/users", new
        {
            username,
            fullName = username,
            email = $"{username}@example.com",
            password,
            isActive = true,
            mustChangePassword = true
        });

        fixture.ClearAuth();
        var tokenContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = username,
            ["password"] = password,
            ["client_id"] = "mobile-app",
            ["scope"] = "openid profile email ws"
        });
        var tokenResponse = await fixture.Client.PostAsync("/connect/token", tokenContent);
        var error = await tokenResponse.Content.ReadFromJsonAsync<OidcErrorResponse>(IntegrationTestFixture.JsonOptions);
        error.Should().NotBeNull();
        error!.ChallengeId.Should().NotBeNullOrWhiteSpace();

        var changeResponse = await fixture.Client.PostAsJsonAsync("/api/account/password/forced-change", new
        {
            challengeId = Guid.Parse(error.ChallengeId!),
            newPassword
        });
        changeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Login with new password should succeed
        var finalLogin = await fixture.LoginAsync(username, newPassword);
        finalLogin.AccessToken.Should().NotBeNullOrWhiteSpace();
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
            fullName = username,
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
        var changeResponse = await fixture.Client.PostAsJsonAsync("/api/account/password/forced-change", new
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
            fullName = username,
            email = $"{username}@example.com",
            password,
            isActive = true,
            mustChangePassword = true
        });

        fixture.ClearAuth();
        var tokenContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = username,
            ["password"] = password,
            ["client_id"] = "mobile-app",
            ["scope"] = "openid profile email ws"
        });
        var tokenResponse = await fixture.Client.PostAsync("/connect/token", tokenContent);
        var error = await tokenResponse.Content.ReadFromJsonAsync<OidcErrorResponse>(IntegrationTestFixture.JsonOptions);
        var challengeId = Guid.Parse(error!.ChallengeId!);

        await fixture.Client.PostAsJsonAsync("/api/account/password/forced-change", new
        {
            challengeId,
            newPassword = "NewSecurePassword1!"
        });

        var secondChangeResponse = await fixture.Client.PostAsJsonAsync("/api/account/password/forced-change", new
        {
            challengeId,
            newPassword = "AnotherPassword1!"
        });

        secondChangeResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ForcedChangePassword_WithRandomGuid_Returns401()
    {
        fixture.ClearAuth();

        var response = await fixture.Client.PostAsJsonAsync("/api/account/password/forced-change", new
        {
            challengeId = Guid.NewGuid(),
            newPassword = "NewSecurePassword1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
