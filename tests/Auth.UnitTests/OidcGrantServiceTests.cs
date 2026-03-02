using System.Security.Claims;
using Auth.Application;
using Auth.Domain;
using Auth.Infrastructure;
using FluentAssertions;
using Moq;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.UnitTests;

public sealed class OidcGrantServiceTests
{
    private static readonly User TestUser = new()
    {
        Id = Guid.NewGuid(),
        Username = "testuser",
        FullName = "Test User",
        Email = "test@example.com",
        Phone = "+1234567890",
        PasswordHash = "hash",
        IsActive = true
    };

    [Fact]
    public async Task HandlePasswordGrantAsync_WhenCredentialsValid_ReturnsSuccessWithPrincipal()
    {
        var authService = new Mock<IAuthService>();
        authService.Setup(x => x.ValidateCredentialsAsync("testuser", "password", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var service = CreateService(authService: authService);

        var result = await service.HandlePasswordGrantAsync(
            "testuser", "password", ["openid", "profile"], CancellationToken.None);

        result.Should().BeOfType<PasswordGrantResult.Success>();
        var success = (PasswordGrantResult.Success)result;
        success.Principal.FindFirst(Claims.Subject)!.Value.Should().Be(TestUser.Id.ToString());
    }

    [Fact]
    public async Task HandlePasswordGrantAsync_WhenMustChangePassword_ReturnsPasswordChangeRequired()
    {
        var user = new User
        {
            Id = Guid.NewGuid(), Username = "mcp", FullName = "MCP", Email = "mcp@test.com",
            PasswordHash = "hash", IsActive = true
        };
        user.MarkMustChangePassword();

        var challengeId = Guid.NewGuid();
        var authService = new Mock<IAuthService>();
        authService.Setup(x => x.ValidateCredentialsAsync("mcp", "pwd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        authService.Setup(x => x.CreatePasswordChangeChallengeAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PasswordChangeChallenge.Create(user.Id, DateTime.UtcNow.AddMinutes(15)));
        var service = CreateService(authService: authService);

        var result = await service.HandlePasswordGrantAsync(
            "mcp", "pwd", ["openid"], CancellationToken.None);

        result.Should().BeOfType<PasswordGrantResult.PasswordChangeRequired>();
    }

    [Fact]
    public async Task HandlePasswordGrantAsync_WhenTwoFactorEnabled_ReturnsMfaRequired()
    {
        var user = new User
        {
            Id = Guid.NewGuid(), Username = "mfa", FullName = "MFA", Email = "mfa@test.com",
            PasswordHash = "hash", IsActive = true
        };
        user.EnableTwoFactor(TwoFactorChannel.Email);

        var challenge = TwoFactorChallenge.Create(
            user.Id, TwoFactorChallenge.PurposeLogin, TwoFactorChannel.Email,
            "hash", "salt", "enc", DateTime.UtcNow.AddMinutes(5), 5);

        var authService = new Mock<IAuthService>();
        authService.Setup(x => x.ValidateCredentialsAsync("mfa", "pwd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        authService.Setup(x => x.CreateLoginChallengeAsync(user.Id, TwoFactorChannel.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);
        var service = CreateService(authService: authService);

        var result = await service.HandlePasswordGrantAsync(
            "mfa", "pwd", ["openid"], CancellationToken.None);

        result.Should().BeOfType<PasswordGrantResult.MfaRequired>();
        var mfa = (PasswordGrantResult.MfaRequired)result;
        mfa.Channel.Should().Be(TwoFactorChannel.Email);
    }

    [Fact]
    public async Task HandlePasswordGrantAsync_WhenInvalidCredentials_ThrowsAuthException()
    {
        var authService = new Mock<IAuthService>();
        authService.Setup(x => x.ValidateCredentialsAsync("bad", "bad", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AuthException(AuthErrorCatalog.InvalidCredentials));
        var service = CreateService(authService: authService);

        var act = () => service.HandlePasswordGrantAsync(
            "bad", "bad", ["openid"], CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.InvalidCredentials);
    }

    [Fact]
    public async Task HandleMfaOtpGrantAsync_WhenValid_ReturnsPrincipal()
    {
        var challengeId = Guid.NewGuid();
        var twoFactorService = new Mock<ITwoFactorAuthService>();
        twoFactorService.Setup(x => x.ValidateLoginOtpAsync(
                challengeId, TwoFactorChannel.Email, "123456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var service = CreateService(twoFactorService: twoFactorService);

        var principal = await service.HandleMfaOtpGrantAsync(
            challengeId, TwoFactorChannel.Email, "123456", ["openid", "profile"], CancellationToken.None);

        principal.FindFirst(Claims.Subject)!.Value.Should().Be(TestUser.Id.ToString());
    }

    [Fact]
    public async Task HandleMfaOtpGrantAsync_WhenInvalidOtp_ThrowsAuthException()
    {
        var challengeId = Guid.NewGuid();
        var twoFactorService = new Mock<ITwoFactorAuthService>();
        twoFactorService.Setup(x => x.ValidateLoginOtpAsync(
                challengeId, TwoFactorChannel.Email, "000000", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AuthException(TwoFactorErrorCatalog.VerificationFailed));
        var service = CreateService(twoFactorService: twoFactorService);

        var act = () => service.HandleMfaOtpGrantAsync(
            challengeId, TwoFactorChannel.Email, "000000", ["openid"], CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == TwoFactorErrorCatalog.VerificationFailed);
    }

    [Fact]
    public async Task BuildPrincipalAsync_SetsCoreClaimsAndDestinations()
    {
        var authService = new Mock<IAuthService>();
        authService.Setup(x => x.GetActiveUserAsync(TestUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var service = CreateService(authService: authService);

        var principal = await service.BuildPrincipalAsync(
            TestUser.Id, ["openid", "profile"], CancellationToken.None);

        principal.FindFirst(Claims.Subject)!.Value.Should().Be(TestUser.Id.ToString());
        principal.FindFirst(Claims.Name)!.Value.Should().Be("Test User");
        principal.FindFirst(Claims.PreferredUsername)!.Value.Should().Be("testuser");
        principal.FindFirst(Claims.Email).Should().BeNull("email scope was not requested");
    }

    [Fact]
    public async Task BuildPrincipalAsync_WhenEmailScope_IncludesEmailClaim()
    {
        var authService = new Mock<IAuthService>();
        authService.Setup(x => x.GetActiveUserAsync(TestUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var service = CreateService(authService: authService);

        var principal = await service.BuildPrincipalAsync(
            TestUser.Id, ["openid", "email"], CancellationToken.None);

        principal.FindFirst(Claims.Email)!.Value.Should().Be("test@example.com");
    }

    [Fact]
    public async Task BuildPrincipalAsync_WhenPhoneScope_IncludesPhoneClaim()
    {
        var authService = new Mock<IAuthService>();
        authService.Setup(x => x.GetActiveUserAsync(TestUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var service = CreateService(authService: authService);

        var principal = await service.BuildPrincipalAsync(
            TestUser.Id, ["openid", "phone"], CancellationToken.None);

        principal.FindFirst(Claims.PhoneNumber)!.Value.Should().Be("+1234567890");
    }

    [Fact]
    public async Task BuildPrincipalAsync_WhenPhoneScopeButNoPhone_OmitsPhoneClaim()
    {
        var userNoPhone = new User
        {
            Id = Guid.NewGuid(), Username = "nophone", FullName = "No Phone",
            Email = "nophone@test.com", PasswordHash = "hash", IsActive = true
        };
        var authService = new Mock<IAuthService>();
        authService.Setup(x => x.GetActiveUserAsync(userNoPhone.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userNoPhone);
        var service = CreateService(authService: authService);

        var principal = await service.BuildPrincipalAsync(
            userNoPhone.Id, ["openid", "phone"], CancellationToken.None);

        principal.FindFirst(Claims.PhoneNumber).Should().BeNull();
    }

    [Fact]
    public async Task BuildPrincipalAsync_WhenWsScope_IncludesWorkspaceMasks()
    {
        var authService = new Mock<IAuthService>();
        authService.Setup(x => x.GetActiveUserAsync(TestUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var workspaceMaskService = new Mock<IWorkspaceMaskService>();
        workspaceMaskService.Setup(x => x.BuildWorkspaceMasksAsync(TestUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, byte[]> { ["system"] = [0b_0000_0101] });
        var service = CreateService(authService: authService, workspaceMaskService: workspaceMaskService);

        var principal = await service.BuildPrincipalAsync(
            TestUser.Id, ["openid", "ws"], CancellationToken.None);

        var wsClaim = principal.FindFirst("ws");
        wsClaim.Should().NotBeNull();
        wsClaim!.Value.Should().Contain("system");
        wsClaim.Value.Should().Contain(Convert.ToBase64String([0b_0000_0101]));
    }

    [Fact]
    public async Task BuildPrincipalAsync_SetsCorrectDestinations()
    {
        var authService = new Mock<IAuthService>();
        authService.Setup(x => x.GetActiveUserAsync(TestUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var service = CreateService(authService: authService);

        var principal = await service.BuildPrincipalAsync(
            TestUser.Id, ["openid", "profile", "email"], CancellationToken.None);

        var subClaim = principal.FindFirst(Claims.Subject)!;
        subClaim.GetDestinations().Should().Contain(Destinations.AccessToken);
        subClaim.GetDestinations().Should().Contain(Destinations.IdentityToken);

        var nameClaim = principal.FindFirst(Claims.Name)!;
        nameClaim.GetDestinations().Should().Contain(Destinations.IdentityToken);
        nameClaim.GetDestinations().Should().NotContain(Destinations.AccessToken);
    }

    private static OidcGrantService CreateService(
        Mock<IAuthService>? authService = null,
        Mock<ITwoFactorAuthService>? twoFactorService = null,
        Mock<IWorkspaceMaskService>? workspaceMaskService = null)
    {
        authService ??= new Mock<IAuthService>();
        twoFactorService ??= new Mock<ITwoFactorAuthService>();
        workspaceMaskService ??= new Mock<IWorkspaceMaskService>();

        return new OidcGrantService(
            authService.Object,
            twoFactorService.Object,
            workspaceMaskService.Object);
    }
}
