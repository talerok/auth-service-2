using Auth.Api;
using Auth.Application;
using Auth.Application.Verification;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace Auth.UnitTests.Api;

public class AuthProblemDetailsMapperTests
{
    [Theory]
    [InlineData(AuthErrorCatalog.AuthenticationRequired, StatusCodes.Status401Unauthorized)]
    [InlineData(AuthErrorCatalog.AuthenticationFailed, StatusCodes.Status401Unauthorized)]
    [InlineData(AuthErrorCatalog.AccessDenied, StatusCodes.Status403Forbidden)]
    [InlineData(AuthErrorCatalog.InvalidCredentials, StatusCodes.Status401Unauthorized)]
    [InlineData(AuthErrorCatalog.UserInactive, StatusCodes.Status403Forbidden)]
    [InlineData(AuthErrorCatalog.UserNotFound, StatusCodes.Status404NotFound)]
    [InlineData(AuthErrorCatalog.InvalidUserContext, StatusCodes.Status401Unauthorized)]
    [InlineData(AuthErrorCatalog.DuplicateIdentity, StatusCodes.Status409Conflict)]
    [InlineData(AuthErrorCatalog.DuplicateIdsNotAllowed, StatusCodes.Status400BadRequest)]
    [InlineData(AuthErrorCatalog.SystemWorkspaceDeleteForbidden, StatusCodes.Status400BadRequest)]
    [InlineData(AuthErrorCatalog.SystemPermissionDeleteForbidden, StatusCodes.Status400BadRequest)]
    [InlineData(AuthErrorCatalog.SystemPermissionImportForbidden, StatusCodes.Status400BadRequest)]
    [InlineData(AuthErrorCatalog.InvalidPasswordChangeChallenge, StatusCodes.Status401Unauthorized)]
    [InlineData(TwoFactorErrorCatalog.UnsupportedChannel, StatusCodes.Status400BadRequest)]
    [InlineData(TwoFactorErrorCatalog.ChallengeNotFound, StatusCodes.Status404NotFound)]
    [InlineData(TwoFactorErrorCatalog.ChallengeExpired, StatusCodes.Status410Gone)]
    [InlineData(TwoFactorErrorCatalog.AttemptsExceeded, StatusCodes.Status429TooManyRequests)]
    [InlineData(TwoFactorErrorCatalog.OtpAlreadyUsed, StatusCodes.Status409Conflict)]
    [InlineData(TwoFactorErrorCatalog.VerificationFailed, StatusCodes.Status401Unauthorized)]
    [InlineData(TwoFactorErrorCatalog.Required, StatusCodes.Status401Unauthorized)]
    [InlineData(TwoFactorErrorCatalog.NotRequired, StatusCodes.Status401Unauthorized)]
    [InlineData(TwoFactorErrorCatalog.ActivationNotCompleted, StatusCodes.Status409Conflict)]
    [InlineData(TwoFactorErrorCatalog.DeliveryFailed, StatusCodes.Status503ServiceUnavailable)]
    [InlineData(TwoFactorErrorCatalog.ProviderUnavailable, StatusCodes.Status503ServiceUnavailable)]
    [InlineData(TwoFactorErrorCatalog.PhoneRequired, StatusCodes.Status400BadRequest)]
    [InlineData(AuthErrorCatalog.IdentitySourceNotFound, StatusCodes.Status404NotFound)]
    [InlineData(AuthErrorCatalog.IdentitySourceDisabled, StatusCodes.Status400BadRequest)]
    [InlineData(AuthErrorCatalog.IdentitySourceTokenInvalid, StatusCodes.Status401Unauthorized)]
    [InlineData(AuthErrorCatalog.IdentitySourceLinkNotFound, StatusCodes.Status401Unauthorized)]
    [InlineData(AuthErrorCatalog.IdentitySourceUserInactive, StatusCodes.Status401Unauthorized)]
    [InlineData(AuthErrorCatalog.IdentitySourceDuplicateLink, StatusCodes.Status400BadRequest)]
    [InlineData(AuthErrorCatalog.IdentitySourceTypeMismatch, StatusCodes.Status400BadRequest)]
    [InlineData(AuthErrorCatalog.ApplicationNotFound, StatusCodes.Status404NotFound)]
    [InlineData(AuthErrorCatalog.ApplicationInactive, StatusCodes.Status403Forbidden)]
    [InlineData(AuthErrorCatalog.PermissionCodeNotFound, StatusCodes.Status400BadRequest)]
    [InlineData(AuthErrorCatalog.SystemWorkspaceImportForbidden, StatusCodes.Status400BadRequest)]
    [InlineData(AuthErrorCatalog.InternalAuthDisabled, StatusCodes.Status403Forbidden)]
    [InlineData(AuthErrorCatalog.ConsentRequired, StatusCodes.Status403Forbidden)]
    [InlineData(AuthErrorCatalog.AuthorizationNotFound, StatusCodes.Status404NotFound)]
    [InlineData(AuthErrorCatalog.InvalidRedirectUri, StatusCodes.Status400BadRequest)]
    [InlineData(VerificationErrorCatalog.InvalidChallenge, StatusCodes.Status404NotFound)]
    [InlineData(VerificationErrorCatalog.ChallengeExpired, StatusCodes.Status410Gone)]
    [InlineData(VerificationErrorCatalog.InvalidOtp, StatusCodes.Status401Unauthorized)]
    [InlineData(VerificationErrorCatalog.MaxAttemptsExceeded, StatusCodes.Status429TooManyRequests)]
    [InlineData(VerificationErrorCatalog.VerificationCooldown, StatusCodes.Status429TooManyRequests)]
    [InlineData(VerificationErrorCatalog.UserNotFound, StatusCodes.Status404NotFound)]
    [InlineData(VerificationErrorCatalog.NoEmailConfigured, StatusCodes.Status400BadRequest)]
    [InlineData(VerificationErrorCatalog.NoPhoneConfigured, StatusCodes.Status400BadRequest)]
    [InlineData(AuthErrorCatalog.SessionNotFound, StatusCodes.Status404NotFound)]
    [InlineData(AuthErrorCatalog.SessionAlreadyRevoked, StatusCodes.Status409Conflict)]
    [InlineData(AuthErrorCatalog.SessionRevoked, StatusCodes.Status403Forbidden)]
    public void Map_KnownCode_ReturnsExpectedStatusCode(string code, int expectedStatusCode)
    {
        var result = AuthProblemDetailsMapper.Map(code);

        result.StatusCode.Should().Be(expectedStatusCode);
    }

    [Fact]
    public void Map_UnknownCode_Returns400()
    {
        var result = AuthProblemDetailsMapper.Map("UNKNOWN_CODE");

        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        result.Title.Should().Be("Business rule violation");
    }
}
