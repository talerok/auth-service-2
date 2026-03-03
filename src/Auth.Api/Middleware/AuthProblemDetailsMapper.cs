using Auth.Application;

namespace Auth.Api;

public static class AuthProblemDetailsMapper
{
    public static AuthProblemDescriptor Map(AuthException exception) => Map(exception.Code);

    public static AuthProblemDescriptor Map(string code)
    {
        return code switch
        {
            AuthErrorCatalog.AuthenticationRequired => new AuthProblemDescriptor(StatusCodes.Status401Unauthorized, "Unauthorized", "Authentication is required"),
            AuthErrorCatalog.AuthenticationFailed => new AuthProblemDescriptor(StatusCodes.Status401Unauthorized, "Unauthorized", "Authentication failed"),
            AuthErrorCatalog.AccessDenied => new AuthProblemDescriptor(StatusCodes.Status403Forbidden, "Forbidden", "Access denied"),
            AuthErrorCatalog.InvalidCredentials => new AuthProblemDescriptor(StatusCodes.Status401Unauthorized, "Unauthorized", "Invalid credentials"),
            AuthErrorCatalog.UserInactive => new AuthProblemDescriptor(StatusCodes.Status403Forbidden, "Forbidden", "User inactive"),
            AuthErrorCatalog.UserNotFound => new AuthProblemDescriptor(StatusCodes.Status404NotFound, "Resource not found", "User not found"),
            AuthErrorCatalog.InvalidUserContext => new AuthProblemDescriptor(StatusCodes.Status401Unauthorized, "Unauthorized", "Invalid user context"),
            AuthErrorCatalog.DuplicateIdentity => new AuthProblemDescriptor(StatusCodes.Status409Conflict, "Conflict", "Username or email already exists"),
            AuthErrorCatalog.DuplicateIdsNotAllowed => new AuthProblemDescriptor(StatusCodes.Status400BadRequest, "Business rule violation", "Duplicate ids are not allowed"),
            AuthErrorCatalog.SystemWorkspaceDeleteForbidden => new AuthProblemDescriptor(StatusCodes.Status400BadRequest, "Business rule violation", "System workspaces cannot be deleted"),
            AuthErrorCatalog.SystemPermissionDeleteForbidden => new AuthProblemDescriptor(StatusCodes.Status400BadRequest, "Business rule violation", "System permissions cannot be deleted"),
            AuthErrorCatalog.InvalidPasswordChangeChallenge => new AuthProblemDescriptor(StatusCodes.Status401Unauthorized, "Unauthorized", "Invalid or expired password change token"),
            TwoFactorErrorCatalog.UnsupportedChannel => new AuthProblemDescriptor(StatusCodes.Status400BadRequest, "Validation error", "Unsupported two-factor channel"),
            TwoFactorErrorCatalog.ChallengeNotFound => new AuthProblemDescriptor(StatusCodes.Status404NotFound, "Resource not found", "Two-factor challenge not found"),
            TwoFactorErrorCatalog.ChallengeExpired => new AuthProblemDescriptor(StatusCodes.Status410Gone, "Challenge expired", "Two-factor challenge expired"),
            TwoFactorErrorCatalog.AttemptsExceeded => new AuthProblemDescriptor(StatusCodes.Status429TooManyRequests, "Too many attempts", "Too many OTP verification attempts"),
            TwoFactorErrorCatalog.OtpAlreadyUsed => new AuthProblemDescriptor(StatusCodes.Status409Conflict, "Conflict", "OTP is already used"),
            TwoFactorErrorCatalog.VerificationFailed => new AuthProblemDescriptor(StatusCodes.Status401Unauthorized, "Unauthorized", "Two-factor verification failed"),
            TwoFactorErrorCatalog.Required => new AuthProblemDescriptor(StatusCodes.Status401Unauthorized, "Unauthorized", "Two-factor delivery is not completed"),
            TwoFactorErrorCatalog.NotRequired => new AuthProblemDescriptor(StatusCodes.Status401Unauthorized, "Unauthorized", "Two-factor is not required for this account"),
            TwoFactorErrorCatalog.ActivationNotCompleted => new AuthProblemDescriptor(StatusCodes.Status409Conflict, "Conflict", "Two-factor delivery is not completed"),
            TwoFactorErrorCatalog.DeliveryFailed => new AuthProblemDescriptor(StatusCodes.Status503ServiceUnavailable, "Service unavailable", "Two-factor delivery failed"),
            TwoFactorErrorCatalog.ProviderUnavailable => new AuthProblemDescriptor(StatusCodes.Status503ServiceUnavailable, "Service unavailable", "Two-factor provider unavailable"),
            TwoFactorErrorCatalog.PhoneRequired => new AuthProblemDescriptor(StatusCodes.Status400BadRequest, "Validation error", "Phone number is required for SMS two-factor"),
            AuthErrorCatalog.IdentitySourceNotFound => new AuthProblemDescriptor(StatusCodes.Status404NotFound, "Resource not found", "Identity source not found"),
            AuthErrorCatalog.IdentitySourceDisabled => new AuthProblemDescriptor(StatusCodes.Status400BadRequest, "Business rule violation", "Identity source is disabled"),
            AuthErrorCatalog.IdentitySourceTokenInvalid => new AuthProblemDescriptor(StatusCodes.Status401Unauthorized, "Unauthorized", "External identity token is invalid"),
            AuthErrorCatalog.IdentitySourceLinkNotFound => new AuthProblemDescriptor(StatusCodes.Status401Unauthorized, "Unauthorized", "User is not linked to this identity source"),
            AuthErrorCatalog.IdentitySourceUserInactive => new AuthProblemDescriptor(StatusCodes.Status401Unauthorized, "Unauthorized", "Linked user is inactive"),
            AuthErrorCatalog.IdentitySourceDuplicateLink => new AuthProblemDescriptor(StatusCodes.Status400BadRequest, "Business rule violation", "This identity link already exists"),
            AuthErrorCatalog.IdentitySourceTypeMismatch => new AuthProblemDescriptor(StatusCodes.Status400BadRequest, "Business rule violation", "Config type does not match identity source type"),
            AuthErrorCatalog.ApiClientNotFound => new AuthProblemDescriptor(StatusCodes.Status404NotFound, "Resource not found", "API client not found"),
            AuthErrorCatalog.ApiClientInactive => new AuthProblemDescriptor(StatusCodes.Status403Forbidden, "Forbidden", "API client is inactive"),
            _ => new AuthProblemDescriptor(StatusCodes.Status400BadRequest, "Business rule violation", "Business rule violation")
        };
    }
}

public sealed record AuthProblemDescriptor(int StatusCode, string Title, string Detail);
