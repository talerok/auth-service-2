using System.Security.Claims;
using Auth.Application;
using Auth.Application.Oidc.Commands.HandleMfaOtpGrant;
using Auth.Application.Oidc.Queries.BuildPrincipal;
using Auth.Application.Sessions.Commands.CreateSession;
using Auth.Application.TwoFactor.Commands.ValidateLoginOtp;
using Auth.Domain;
using Auth.Infrastructure.Oidc.Commands.HandleMfaOtpGrant;
using FluentAssertions;
using MediatR;
using Moq;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static Auth.UnitTests.Oidc.OidcTestHelpers;

namespace Auth.UnitTests.Oidc;

public sealed class HandleMfaOtpGrantTests
{
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
}
