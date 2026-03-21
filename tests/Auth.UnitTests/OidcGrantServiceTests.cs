using System.Security.Claims;
using Auth.Application;
using Auth.Application.Auth.Commands.CreateLoginChallenge;
using Auth.Application.Auth.Commands.CreatePasswordChangeChallenge;
using Auth.Application.Auth.Commands.ValidateCredentials;
using Auth.Application.Auth.Queries.GetActiveUser;
using Auth.Application.Oidc.Commands.HandleClientCredentialsGrant;
using Auth.Application.Oidc.Commands.HandleMfaOtpGrant;
using Auth.Application.Oidc.Commands.ValidateCredentialsForLogin;
using Auth.Application.Oidc.Queries.BuildPrincipal;
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
using Microsoft.EntityFrameworkCore;
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
        IsActive = true,
        IsInternalAuthEnabled = true
    };

    // ValidateCredentialsForLoginCommandHandler tests

    [Fact]
    public async Task ValidateCredentialsForLogin_WhenCredentialsValid_ReturnsSuccessWithPrincipal()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<ValidateCredentialsCommand>(c => c.Username == "testuser" && c.Password == "password"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var handler = new ValidateCredentialsForLoginCommandHandler(sender.Object);

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
        var handler = new ValidateCredentialsForLoginCommandHandler(sender.Object);

        var result = await handler.Handle(
            new ValidateCredentialsForLoginCommand("mcp", "pwd", ["openid"]), CancellationToken.None);

        result.Should().BeOfType<CredentialValidationResult.PasswordChangeRequired>();
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
        var handler = new ValidateCredentialsForLoginCommandHandler(sender.Object);

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
        var handler = new ValidateCredentialsForLoginCommandHandler(sender.Object);

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
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<ValidateLoginOtpCommand>(c => c.ChallengeId == challengeId && c.Channel == TwoFactorChannel.Email && c.Otp == "123456"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var handler = new HandleMfaOtpGrantCommandHandler(sender.Object);

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
        var handler = new HandleMfaOtpGrantCommandHandler(sender.Object);

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
        var handler = new BuildPrincipalQueryHandler(sender.Object);

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "profile"]), CancellationToken.None);

        principal.FindFirst(Claims.Subject)!.Value.Should().Be(TestUser.Id.ToString());
        principal.FindFirst(Claims.Name)!.Value.Should().Be("Test User");
        principal.FindFirst(Claims.PreferredUsername)!.Value.Should().Be("testuser");
        principal.FindFirst(Claims.Email).Should().BeNull("email scope was not requested");
    }

    [Fact]
    public async Task BuildPrincipal_WhenEmailScope_IncludesEmailClaim()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var handler = new BuildPrincipalQueryHandler(sender.Object);

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
        var handler = new BuildPrincipalQueryHandler(sender.Object);

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
        var handler = new BuildPrincipalQueryHandler(sender.Object);

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
        var handler = new BuildPrincipalQueryHandler(sender.Object);

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "ws"]), CancellationToken.None);

        var wsClaim = principal.FindFirst("ws");
        wsClaim.Should().NotBeNull();
        wsClaim!.Value.Should().Contain("system");
        wsClaim.Value.Should().Contain(Convert.ToBase64String([0b_0000_0101]));
    }

    [Fact]
    public async Task BuildPrincipal_SetsCorrectDestinations()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var handler = new BuildPrincipalQueryHandler(sender.Object);

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "profile", "email"]), CancellationToken.None);

        var subClaim = principal.FindFirst(Claims.Subject)!;
        subClaim.GetDestinations().Should().Contain(Destinations.AccessToken);
        subClaim.GetDestinations().Should().Contain(Destinations.IdentityToken);

        var nameClaim = principal.FindFirst(Claims.Name)!;
        nameClaim.GetDestinations().Should().Contain(Destinations.IdentityToken);
        nameClaim.GetDestinations().Should().NotContain(Destinations.AccessToken);
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
        var handler = new HandleClientCredentialsGrantCommandHandler(sender.Object, dbContext);

        var principal = await handler.Handle(
            new HandleClientCredentialsGrantCommand("ws-client", ["openid", "ws"]), CancellationToken.None);

        var wsClaim = principal.FindFirst("ws");
        wsClaim.Should().NotBeNull();
        wsClaim!.Value.Should().Contain("system");
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AuthDbContext(options);
    }
}
