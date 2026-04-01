using System.Security.Claims;
using Auth.Application;
using Auth.Application.Auth.Commands.CreateLoginChallenge;
using Auth.Application.Auth.Commands.CreatePasswordChangeChallenge;
using Auth.Application.Auth.Commands.ValidateCredentials;
using Auth.Application.Oidc.Commands.ValidateCredentialsForLogin;
using Auth.Application.Oidc.Queries.BuildPrincipal;
using Auth.Application.Sessions.Commands.CreateSession;
using Auth.Domain;
using Auth.Infrastructure.Oidc.Commands.ValidateCredentialsForLogin;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Options;
using Moq;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static Auth.UnitTests.Oidc.OidcTestHelpers;

namespace Auth.UnitTests.Oidc;

public sealed class ValidateCredentialsForLoginTests
{
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
}
