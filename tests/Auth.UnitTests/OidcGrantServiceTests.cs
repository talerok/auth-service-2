using System.Security.Claims;
using Auth.Application;
using Auth.Application.Auth.Commands.CreateLoginChallenge;
using Auth.Application.Auth.Commands.CreatePasswordChangeChallenge;
using Auth.Application.Auth.Commands.ValidateCredentials;
using Auth.Application.Auth.Queries.GetActiveUser;
using Auth.Application.Oidc.Queries.GetApplicationAudiences;
using Auth.Application.Oidc.Commands.HandleClientCredentialsGrant;
using Auth.Application.Oidc.Commands.HandleMfaOtpGrant;
using Auth.Application.Oidc.Commands.ValidateCredentialsForLogin;
using Auth.Application.Oidc.Queries.BuildPrincipal;
using Auth.Application.Sessions.Commands.CreateSession;
using Auth.Application.TwoFactor.Commands.ValidateLoginOtp;
using Auth.Application.Workspaces.Queries.BuildServiceAccountWorkspaceMasks;
using Auth.Application.Workspaces.Queries.BuildWorkspaceMasks;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Oidc.Commands.HandleClientCredentialsGrant;
using Auth.Infrastructure.Oidc.Commands.HandleMfaOtpGrant;
using Auth.Infrastructure.Oidc.Commands.ValidateCredentialsForLogin;
using Auth.Infrastructure.Oidc.Queries.BuildPrincipal;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static Auth.UnitTests.TestDbContextFactory;

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
        IsActive = true,
        IsInternalAuthEnabled = true
    };

    private static Mock<IHttpContextAccessor> CreateHttpContextAccessor()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        httpContext.Request.Headers["User-Agent"] = "TestAgent/1.0";
        var mock = new Mock<IHttpContextAccessor>();
        mock.Setup(x => x.HttpContext).Returns(httpContext);
        return mock;
    }

    // ValidateCredentialsForLoginCommandHandler tests

    [Fact]
    public async Task ValidateCredentialsForLogin_WhenCredentialsValid_ReturnsSuccessWithPrincipal()
    {
        var expectedPrincipal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(Claims.Subject, TestUser.Id.ToString())]));
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<ValidateCredentialsCommand>(c => c.Username == "testuser" && c.Password == "password"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        sender.Setup(x => x.Send(It.IsAny<CreateSessionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        sender.Setup(x => x.Send(It.IsAny<BuildPrincipalQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPrincipal);
        var handler = new ValidateCredentialsForLoginCommandHandler(sender.Object, CreateHttpContextAccessor().Object, Options.Create(new PasswordExpirationOptions()));

        var result = await handler.Handle(
            new ValidateCredentialsForLoginCommand("testuser", "password", ["openid", "profile"]), CancellationToken.None);

        result.Should().BeOfType<CredentialValidationResult.Success>();
        var success = (CredentialValidationResult.Success)result;
        success.Principal.FindFirst(Claims.Subject)!.Value.Should().Be(TestUser.Id.ToString());
    }

    [Fact]
    public async Task ValidateCredentialsForLogin_WhenMustChangePassword_ReturnsPasswordChangeRequired()
    {
        var user = new User
        {
            Id = Guid.NewGuid(), Username = "mcp", FullName = "MCP", Email = "mcp@test.com",
            PasswordHash = "hash", IsActive = true
        };
        user.MarkMustChangePassword();

        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<ValidateCredentialsCommand>(c => c.Username == "mcp" && c.Password == "pwd"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        sender.Setup(x => x.Send(
                It.Is<CreatePasswordChangeChallengeCommand>(c => c.UserId == user.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PasswordChangeChallenge.Create(user.Id, DateTime.UtcNow.AddMinutes(15)));
        var handler = new ValidateCredentialsForLoginCommandHandler(sender.Object, CreateHttpContextAccessor().Object, Options.Create(new PasswordExpirationOptions()));

        var result = await handler.Handle(
            new ValidateCredentialsForLoginCommand("mcp", "pwd", ["openid"]), CancellationToken.None);

        result.Should().BeOfType<CredentialValidationResult.PasswordChangeRequired>();
    }

    [Fact]
    public async Task ValidateCredentialsForLogin_WhenPasswordExpired_ReturnsPasswordChangeRequired()
    {
        var user = new User
        {
            Id = Guid.NewGuid(), Username = "expired", FullName = "Expired", Email = "expired@test.com",
            PasswordHash = "hash", IsActive = true
        };
        user.SetPassword("hash");
        // Simulate password changed 100 days ago via EF-style approach
        typeof(User).GetProperty("PasswordChangedAt")!.SetValue(user, DateTime.UtcNow.AddDays(-100));

        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<ValidateCredentialsCommand>(c => c.Username == "expired" && c.Password == "pwd"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        sender.Setup(x => x.Send(
                It.Is<CreatePasswordChangeChallengeCommand>(c => c.UserId == user.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PasswordChangeChallenge.Create(user.Id, DateTime.UtcNow.AddMinutes(15)));
        var options = Options.Create(new PasswordExpirationOptions { DefaultMaxAgeDays = 90 });
        var handler = new ValidateCredentialsForLoginCommandHandler(sender.Object, CreateHttpContextAccessor().Object, options);

        var result = await handler.Handle(
            new ValidateCredentialsForLoginCommand("expired", "pwd", ["openid"]), CancellationToken.None);

        result.Should().BeOfType<CredentialValidationResult.PasswordChangeRequired>();
    }

    [Fact]
    public async Task ValidateCredentialsForLogin_WhenExpirationDisabled_ReturnsSuccess()
    {
        var user = new User
        {
            Id = Guid.NewGuid(), Username = "noexp", FullName = "NoExp", Email = "noexp@test.com",
            PasswordHash = "hash", IsActive = true
        };
        user.SetPassword("hash");
        // Password changed 200 days ago, but expiration is disabled (0)
        typeof(User).GetProperty("PasswordChangedAt")!.SetValue(user, DateTime.UtcNow.AddDays(-200));

        var expectedPrincipal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(Claims.Subject, user.Id.ToString())]));
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<ValidateCredentialsCommand>(c => c.Username == "noexp"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        sender.Setup(x => x.Send(It.IsAny<CreateSessionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        sender.Setup(x => x.Send(It.IsAny<BuildPrincipalQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPrincipal);
        var options = Options.Create(new PasswordExpirationOptions { DefaultMaxAgeDays = 0 });
        var handler = new ValidateCredentialsForLoginCommandHandler(sender.Object, CreateHttpContextAccessor().Object, options);

        var result = await handler.Handle(
            new ValidateCredentialsForLoginCommand("noexp", "pwd", ["openid"]), CancellationToken.None);

        result.Should().BeOfType<CredentialValidationResult.Success>();
    }

    [Fact]
    public async Task ValidateCredentialsForLogin_WhenTwoFactorEnabled_ReturnsMfaRequired()
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

        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<ValidateCredentialsCommand>(c => c.Username == "mfa" && c.Password == "pwd"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        sender.Setup(x => x.Send(
                It.Is<CreateLoginChallengeCommand>(c => c.UserId == user.Id && c.Channel == TwoFactorChannel.Email),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);
        var handler = new ValidateCredentialsForLoginCommandHandler(sender.Object, CreateHttpContextAccessor().Object, Options.Create(new PasswordExpirationOptions()));

        var result = await handler.Handle(
            new ValidateCredentialsForLoginCommand("mfa", "pwd", ["openid"]), CancellationToken.None);

        result.Should().BeOfType<CredentialValidationResult.MfaRequired>();
        var mfa = (CredentialValidationResult.MfaRequired)result;
        mfa.Channel.Should().Be(TwoFactorChannel.Email);
    }

    [Fact]
    public async Task ValidateCredentialsForLogin_WhenInvalidCredentials_ThrowsAuthException()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<ValidateCredentialsCommand>(c => c.Username == "bad" && c.Password == "bad"),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AuthException(AuthErrorCatalog.InvalidCredentials));
        var handler = new ValidateCredentialsForLoginCommandHandler(sender.Object, CreateHttpContextAccessor().Object, Options.Create(new PasswordExpirationOptions()));

        var act = () => handler.Handle(
            new ValidateCredentialsForLoginCommand("bad", "bad", ["openid"]), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.InvalidCredentials);
    }

    // HandleMfaOtpGrantCommandHandler tests

    [Fact]
    public async Task HandleMfaOtpGrant_WhenValid_ReturnsPrincipal()
    {
        var challengeId = Guid.NewGuid();
        var expectedPrincipal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(Claims.Subject, TestUser.Id.ToString())]));
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<ValidateLoginOtpCommand>(c => c.ChallengeId == challengeId && c.Channel == TwoFactorChannel.Email && c.Otp == "123456"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        sender.Setup(x => x.Send(It.IsAny<CreateSessionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        sender.Setup(x => x.Send(It.IsAny<BuildPrincipalQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPrincipal);
        var handler = new HandleMfaOtpGrantCommandHandler(sender.Object, CreateHttpContextAccessor().Object);

        var principal = await handler.Handle(
            new HandleMfaOtpGrantCommand(challengeId.ToString(), TwoFactorChannel.Email.ToString(), "123456", ["openid", "profile"]),
            CancellationToken.None);

        principal.FindFirst(Claims.Subject)!.Value.Should().Be(TestUser.Id.ToString());
    }

    [Fact]
    public async Task HandleMfaOtpGrant_WhenInvalidOtp_ThrowsAuthException()
    {
        var challengeId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<ValidateLoginOtpCommand>(c => c.ChallengeId == challengeId && c.Channel == TwoFactorChannel.Email && c.Otp == "000000"),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AuthException(TwoFactorErrorCatalog.VerificationFailed));
        var handler = new HandleMfaOtpGrantCommandHandler(sender.Object, CreateHttpContextAccessor().Object);

        var act = () => handler.Handle(
            new HandleMfaOtpGrantCommand(challengeId.ToString(), TwoFactorChannel.Email.ToString(), "000000", ["openid"]),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == TwoFactorErrorCatalog.VerificationFailed);
    }

    // BuildPrincipalQueryHandler tests

    [Fact]
    public async Task BuildPrincipal_SetsCoreClaimsAndDestinations()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var handler = new BuildPrincipalQueryHandler(sender.Object, Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "profile"]), CancellationToken.None);

        principal.FindFirst(Claims.Subject)!.Value.Should().Be(TestUser.Id.ToString());
        principal.FindFirst(Claims.Name)!.Value.Should().Be("Test User");
        principal.FindFirst(Claims.PreferredUsername)!.Value.Should().Be("testuser");
        principal.FindFirst(Claims.Email).Should().BeNull("email scope was not requested");
        principal.FindFirst(Claims.AuthenticationTime).Should().NotBeNull();
    }

    [Fact]
    public async Task BuildPrincipal_WhenEmailScope_IncludesEmailClaim()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var handler = new BuildPrincipalQueryHandler(sender.Object, Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "email"]), CancellationToken.None);

        principal.FindFirst(Claims.Email)!.Value.Should().Be("test@example.com");
    }

    [Fact]
    public async Task BuildPrincipal_WhenPhoneScope_IncludesPhoneClaim()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var handler = new BuildPrincipalQueryHandler(sender.Object, Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "phone"]), CancellationToken.None);

        principal.FindFirst(Claims.PhoneNumber)!.Value.Should().Be("+1234567890");
    }

    [Fact]
    public async Task BuildPrincipal_WhenPhoneScopeButNoPhone_OmitsPhoneClaim()
    {
        var userNoPhone = new User
        {
            Id = Guid.NewGuid(), Username = "nophone", FullName = "No Phone",
            Email = "nophone@test.com", PasswordHash = "hash", IsActive = true
        };
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == userNoPhone.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userNoPhone);
        var handler = new BuildPrincipalQueryHandler(sender.Object, Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(userNoPhone.Id, ["openid", "phone"]), CancellationToken.None);

        principal.FindFirst(Claims.PhoneNumber).Should().BeNull();
    }

    [Fact]
    public async Task BuildPrincipal_WhenWsScope_IncludesWorkspaceMasks()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        sender.Setup(x => x.Send(
                It.Is<BuildWorkspaceMasksQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Dictionary<string, byte[]>> { ["system"] = new() { ["system"] = [0b_0000_0101] } });
        var handler = new BuildPrincipalQueryHandler(sender.Object, Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "ws:system"]), CancellationToken.None);

        var wsClaim = principal.FindFirst("ws:system");
        wsClaim.Should().NotBeNull();
        wsClaim!.Value.Should().Contain("system");
        wsClaim.Value.Should().Contain(Convert.ToBase64String([0b_0000_0101]));
    }

    [Fact]
    public async Task BuildPrincipal_WhenMultipleWsScopes_IncludesOnlyRequested()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        sender.Setup(x => x.Send(
                It.Is<BuildWorkspaceMasksQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Dictionary<string, byte[]>>
            {
                ["system"] = new() { ["system"] = [0x01] },
                ["dev"] = new() { ["system"] = [0x02] },
                ["other"] = new() { ["system"] = [0x04] }
            });
        var handler = new BuildPrincipalQueryHandler(sender.Object, Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "ws:system", "ws:dev"]), CancellationToken.None);

        principal.FindFirst("ws:system").Should().NotBeNull();
        principal.FindFirst("ws:dev").Should().NotBeNull();
        principal.FindFirst("ws:other").Should().BeNull();
    }

    [Fact]
    public async Task BuildPrincipal_WhenWsScopeForInaccessibleWorkspace_OmitsFromClaim()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        sender.Setup(x => x.Send(
                It.Is<BuildWorkspaceMasksQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Dictionary<string, byte[]>>());
        var handler = new BuildPrincipalQueryHandler(sender.Object, Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "ws:unknown"]), CancellationToken.None);

        principal.FindFirst("ws:unknown").Should().BeNull();
    }

    [Fact]
    public async Task BuildPrincipal_WhenNoWsScope_DoesNotIncludeWsClaim()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var handler = new BuildPrincipalQueryHandler(sender.Object, Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "profile"]), CancellationToken.None);

        principal.Claims.Should().NotContain(c => c.Type.StartsWith("ws:"));
        sender.Verify(x => x.Send(It.IsAny<BuildWorkspaceMasksQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BuildPrincipal_SetsCorrectDestinations()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var handler = new BuildPrincipalQueryHandler(sender.Object, Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "profile", "email"]), CancellationToken.None);

        var subClaim = principal.FindFirst(Claims.Subject)!;
        subClaim.GetDestinations().Should().Contain(Destinations.AccessToken);
        subClaim.GetDestinations().Should().Contain(Destinations.IdentityToken);

        var nameClaim = principal.FindFirst(Claims.Name)!;
        nameClaim.GetDestinations().Should().Contain(Destinations.IdentityToken);
        nameClaim.GetDestinations().Should().Contain(Destinations.AccessToken);

        var authTimeClaim = principal.FindFirst(Claims.AuthenticationTime)!;
        authTimeClaim.GetDestinations().Should().Contain(Destinations.IdentityToken);
        authTimeClaim.GetDestinations().Should().NotContain(Destinations.AccessToken);
    }

    [Fact]
    public async Task BuildPrincipal_WhenAuthMethodsProvided_IncludesAmrClaims()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var handler = new BuildPrincipalQueryHandler(sender.Object, Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "profile"], AuthMethods: ["pwd", "otp"]), CancellationToken.None);

        var amrClaims = principal.FindAll(Claims.AuthenticationMethodReference).Select(c => c.Value).ToList();
        amrClaims.Should().BeEquivalentTo(["pwd", "otp"]);
    }

    [Fact]
    public async Task BuildPrincipal_WhenNoAuthMethods_OmitsAmrClaims()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var handler = new BuildPrincipalQueryHandler(sender.Object, Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "profile"]), CancellationToken.None);

        principal.FindAll(Claims.AuthenticationMethodReference).Should().BeEmpty();
    }

    [Fact]
    public async Task BuildPrincipal_AmrClaimsDestination_IsIdentityToken()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var handler = new BuildPrincipalQueryHandler(sender.Object, Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "profile"], AuthMethods: ["pwd"]), CancellationToken.None);

        var amrClaim = principal.FindFirst(Claims.AuthenticationMethodReference)!;
        amrClaim.GetDestinations().Should().Contain(Destinations.IdentityToken);
        amrClaim.GetDestinations().Should().NotContain(Destinations.AccessToken);
    }

    [Fact]
    public async Task BuildPrincipal_WhenExpirationActive_IncludesPwdExpClaim()
    {
        var user = new User
        {
            Id = Guid.NewGuid(), Username = "expuser", FullName = "Exp User",
            Email = "exp@test.com", IsActive = true
        };
        user.SetPassword("hash");

        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == user.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var handler = new BuildPrincipalQueryHandler(sender.Object, Options.Create(new PasswordExpirationOptions { DefaultMaxAgeDays = 90 }));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(user.Id, ["openid", "profile"]), CancellationToken.None);

        var pwdExpClaim = principal.FindFirst("pwd_exp");
        pwdExpClaim.Should().NotBeNull();
        var expected = new DateTimeOffset(user.PasswordChangedAt!.Value.AddDays(90), TimeSpan.Zero).ToUnixTimeSeconds();
        long.Parse(pwdExpClaim!.Value).Should().Be(expected);
    }

    [Fact]
    public async Task BuildPrincipal_WhenExpirationDisabled_NoPwdExpClaim()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var handler = new BuildPrincipalQueryHandler(sender.Object, Options.Create(new PasswordExpirationOptions { DefaultMaxAgeDays = 0 }));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "profile"]), CancellationToken.None);

        principal.FindFirst("pwd_exp").Should().BeNull();
    }

    [Fact]
    public async Task BuildPrincipal_PwdExpClaim_GoesToBothTokens()
    {
        var user = new User
        {
            Id = Guid.NewGuid(), Username = "destuser", FullName = "Dest User",
            Email = "dest@test.com", IsActive = true
        };
        user.SetPassword("hash");

        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == user.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var handler = new BuildPrincipalQueryHandler(sender.Object, Options.Create(new PasswordExpirationOptions { DefaultMaxAgeDays = 90 }));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(user.Id, ["openid", "profile"]), CancellationToken.None);

        var pwdExpClaim = principal.FindFirst("pwd_exp")!;
        pwdExpClaim.GetDestinations().Should().Contain(Destinations.AccessToken);
        pwdExpClaim.GetDestinations().Should().Contain(Destinations.IdentityToken);
    }

    // HandleClientCredentialsGrantCommandHandler tests

    [Fact]
    public async Task HandleClientCredentialsGrant_WhenValid_ReturnsPrincipal()
    {
        var dbContext = CreateDbContext();
        var serviceAccount = new Domain.ServiceAccount
        {
            Name = "Test Client",
            ClientId = "test-client",
            IsActive = true
        };
        dbContext.ServiceAccounts.Add(serviceAccount);
        await dbContext.SaveChangesAsync();

        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(It.IsAny<GetApplicationAudiencesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        var handler = new HandleClientCredentialsGrantCommandHandler(sender.Object, dbContext);

        var principal = await handler.Handle(
            new HandleClientCredentialsGrantCommand("test-client", ["openid"]), CancellationToken.None);

        principal.FindFirst(Claims.Subject)!.Value.Should().Be(serviceAccount.Id.ToString());
        principal.FindFirst(Claims.Name)!.Value.Should().Be("Test Client");
    }

    [Fact]
    public async Task HandleClientCredentialsGrant_WhenClientNotFound_ThrowsAuthException()
    {
        var dbContext = CreateDbContext();
        var sender = new Mock<ISender>();
        var handler = new HandleClientCredentialsGrantCommandHandler(sender.Object, dbContext);

        var act = () => handler.Handle(
            new HandleClientCredentialsGrantCommand("nonexistent", ["openid"]), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.ApplicationNotFound);
    }

    [Fact]
    public async Task HandleClientCredentialsGrant_WhenClientInactive_ThrowsAuthException()
    {
        var dbContext = CreateDbContext();
        dbContext.ServiceAccounts.Add(new Domain.ServiceAccount
        {
            Name = "Inactive Client",
            ClientId = "inactive-client",
            IsActive = false
        });
        await dbContext.SaveChangesAsync();

        var sender = new Mock<ISender>();
        var handler = new HandleClientCredentialsGrantCommandHandler(sender.Object, dbContext);

        var act = () => handler.Handle(
            new HandleClientCredentialsGrantCommand("inactive-client", ["openid"]), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.ApplicationInactive);
    }

    [Fact]
    public async Task BuildPrincipal_WhenWildcardWsScope_IncludesAllAccessibleWorkspaces()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        sender.Setup(x => x.Send(
                It.Is<BuildWorkspaceMasksQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Dictionary<string, byte[]>>
            {
                ["system"] = new() { ["system"] = [0x01] },
                ["dev"] = new() { ["system"] = [0x02] },
                ["staging"] = new() { ["system"] = [0x04] }
            });
        var handler = new BuildPrincipalQueryHandler(sender.Object, Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "ws:*"]), CancellationToken.None);

        principal.FindFirst("ws:system").Should().NotBeNull();
        principal.FindFirst("ws:dev").Should().NotBeNull();
        principal.FindFirst("ws:staging").Should().NotBeNull();
    }

    [Fact]
    public async Task BuildPrincipal_WhenWildcardWsScopeButNoAccess_NoClaims()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        sender.Setup(x => x.Send(
                It.Is<BuildWorkspaceMasksQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Dictionary<string, byte[]>>());
        var handler = new BuildPrincipalQueryHandler(sender.Object, Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "ws:*"]), CancellationToken.None);

        principal.Claims.Should().NotContain(c => c.Type.StartsWith("ws:"));
    }

    [Fact]
    public async Task HandleClientCredentialsGrant_WhenWildcardWsScope_IncludesAllWorkspaces()
    {
        var dbContext = CreateDbContext();
        var serviceAccount = new Domain.ServiceAccount
        {
            Name = "Wildcard Client",
            ClientId = "wildcard-client",
            IsActive = true
        };
        dbContext.ServiceAccounts.Add(serviceAccount);
        await dbContext.SaveChangesAsync();

        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<BuildServiceAccountWorkspaceMasksQuery>(q => q.ServiceAccountId == serviceAccount.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Dictionary<string, byte[]>>
            {
                ["system"] = new() { ["system"] = [0x01] },
                ["dev"] = new() { ["system"] = [0x02] }
            });
        sender.Setup(x => x.Send(It.IsAny<GetApplicationAudiencesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        var handler = new HandleClientCredentialsGrantCommandHandler(sender.Object, dbContext);

        var principal = await handler.Handle(
            new HandleClientCredentialsGrantCommand("wildcard-client", ["openid", "ws:*"]), CancellationToken.None);

        principal.FindFirst("ws:system").Should().NotBeNull();
        principal.FindFirst("ws:dev").Should().NotBeNull();
    }

    [Fact]
    public async Task HandleClientCredentialsGrant_WhenWsScope_IncludesWorkspaceMasks()
    {
        var dbContext = CreateDbContext();
        var serviceAccount = new Domain.ServiceAccount
        {
            Name = "WS Client",
            ClientId = "ws-client",
            IsActive = true
        };
        dbContext.ServiceAccounts.Add(serviceAccount);
        await dbContext.SaveChangesAsync();

        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<BuildServiceAccountWorkspaceMasksQuery>(q => q.ServiceAccountId == serviceAccount.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Dictionary<string, byte[]>> { ["system"] = new() { ["system"] = [0b_0000_0011] } });
        sender.Setup(x => x.Send(It.IsAny<GetApplicationAudiencesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        var handler = new HandleClientCredentialsGrantCommandHandler(sender.Object, dbContext);

        var principal = await handler.Handle(
            new HandleClientCredentialsGrantCommand("ws-client", ["openid", "ws:system"]), CancellationToken.None);

        var wsClaim = principal.FindFirst("ws:system");
        wsClaim.Should().NotBeNull();
        wsClaim!.Value.Should().Contain("system");
    }

}
