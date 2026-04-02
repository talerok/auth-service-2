using Auth.Application;
using Auth.Application.Verification;

namespace Auth.Api;

public static class AuthProblemDetailsMapper
{
    private const string Unauthorized = "Unauthorized";
    private const string Forbidden = "Forbidden";
    private const string NotFound = "Resource not found";
    private const string Conflict = "Conflict";
    private const string BusinessRuleViolation = "Business rule violation";
    private const string ValidationError = "Validation error";

    public static AuthProblemDescriptor Map(string code)
    {
        return code switch
        {
            AuthErrorCatalog.AuthenticationRequired => new AuthProblemDescriptor(StatusCodes.Status401Unauthorized, Unauthorized, "Authentication is required"),
            AuthErrorCatalog.AuthenticationFailed => new AuthProblemDescriptor(StatusCodes.Status401Unauthorized, Unauthorized, "Authentication failed"),
            AuthErrorCatalog.AccessDenied => new AuthProblemDescriptor(StatusCodes.Status403Forbidden, Forbidden, "Access denied"),
            AuthErrorCatalog.InvalidCredentials => new AuthProblemDescriptor(StatusCodes.Status401Unauthorized, Unauthorized, "Invalid credentials"),
            AuthErrorCatalog.UserInactive => new AuthProblemDescriptor(StatusCodes.Status403Forbidden, Forbidden, "User inactive"),
            AuthErrorCatalog.UserNotFound => new AuthProblemDescriptor(StatusCodes.Status404NotFound, NotFound, "User not found"),
            AuthErrorCatalog.InvalidUserContext => new AuthProblemDescriptor(StatusCodes.Status401Unauthorized, Unauthorized, "Invalid user context"),
            AuthErrorCatalog.DuplicateIdentity => new AuthProblemDescriptor(StatusCodes.Status409Conflict, Conflict, "Username or email already exists"),
            AuthErrorCatalog.DuplicateIdsNotAllowed => new AuthProblemDescriptor(StatusCodes.Status400BadRequest, BusinessRuleViolation, "Duplicate ids are not allowed"),
            AuthErrorCatalog.SystemWorkspaceDeleteForbidden => new AuthProblemDescriptor(StatusCodes.Status400BadRequest, BusinessRuleViolation, "System workspaces cannot be deleted"),
            AuthErrorCatalog.SystemPermissionDeleteForbidden => new AuthProblemDescriptor(StatusCodes.Status400BadRequest, BusinessRuleViolation, "System permissions cannot be deleted"),
            AuthErrorCatalog.SystemPermissionImportForbidden => new AuthProblemDescriptor(StatusCodes.Status400BadRequest, BusinessRuleViolation, "Cannot import over system permissions"),
            AuthErrorCatalog.InvalidPasswordChangeChallenge => new AuthProblemDescriptor(StatusCodes.Status401Unauthorized, Unauthorized, "Invalid or expired password change token"),
            TwoFactorErrorCatalog.UnsupportedChannel => new AuthProblemDescriptor(StatusCodes.Status400BadRequest, ValidationError, "Unsupported two-factor channel"),
            TwoFactorErrorCatalog.ChallengeNotFound => new AuthProblemDescriptor(StatusCodes.Status404NotFound, NotFound, "Two-factor challenge not found"),
            TwoFactorErrorCatalog.ChallengeExpired => new AuthProblemDescriptor(StatusCodes.Status410Gone, "Challenge expired", "Two-factor challenge expired"),
            TwoFactorErrorCatalog.AttemptsExceeded => new AuthProblemDescriptor(StatusCodes.Status429TooManyRequests, "Too many attempts", "Too many OTP verification attempts"),
            TwoFactorErrorCatalog.OtpAlreadyUsed => new AuthProblemDescriptor(StatusCodes.Status409Conflict, Conflict, "OTP is already used"),
            TwoFactorErrorCatalog.VerificationFailed => new AuthProblemDescriptor(StatusCodes.Status401Unauthorized, Unauthorized, "Two-factor verification failed"),
            TwoFactorErrorCatalog.Required => new AuthProblemDescriptor(StatusCodes.Status401Unauthorized, Unauthorized, "Two-factor delivery is not completed"),
            TwoFactorErrorCatalog.NotRequired => new AuthProblemDescriptor(StatusCodes.Status401Unauthorized, Unauthorized, "Two-factor is not required for this account"),
            TwoFactorErrorCatalog.ActivationNotCompleted => new AuthProblemDescriptor(StatusCodes.Status409Conflict, Conflict, "Two-factor delivery is not completed"),
            TwoFactorErrorCatalog.DeliveryFailed => new AuthProblemDescriptor(StatusCodes.Status503ServiceUnavailable, "Service unavailable", "Two-factor delivery failed"),
            TwoFactorErrorCatalog.ProviderUnavailable => new AuthProblemDescriptor(StatusCodes.Status503ServiceUnavailable, "Service unavailable", "Two-factor provider unavailable"),
            TwoFactorErrorCatalog.PhoneRequired => new AuthProblemDescriptor(StatusCodes.Status400BadRequest, ValidationError, "Phone number is required for SMS two-factor"),
            AuthErrorCatalog.IdentitySourceNotFound => new AuthProblemDescriptor(StatusCodes.Status404NotFound, NotFound, "Identity source not found"),
            AuthErrorCatalog.IdentitySourceDisabled => new AuthProblemDescriptor(StatusCodes.Status400BadRequest, BusinessRuleViolation, "Identity source is disabled"),
            AuthErrorCatalog.IdentitySourceTokenInvalid => new AuthProblemDescriptor(StatusCodes.Status401Unauthorized, Unauthorized, "External identity token is invalid"),
            AuthErrorCatalog.IdentitySourceLinkNotFound => new AuthProblemDescriptor(StatusCodes.Status401Unauthorized, Unauthorized, "User is not linked to this identity source"),
            AuthErrorCatalog.IdentitySourceUserInactive => new AuthProblemDescriptor(StatusCodes.Status401Unauthorized, Unauthorized, "Linked user is inactive"),
            AuthErrorCatalog.IdentitySourceDuplicateLink => new AuthProblemDescriptor(StatusCodes.Status400BadRequest, BusinessRuleViolation, "This identity link already exists"),
            AuthErrorCatalog.IdentitySourceTypeMismatch => new AuthProblemDescriptor(StatusCodes.Status400BadRequest, BusinessRuleViolation, "Config type does not match identity source type"),
            AuthErrorCatalog.ApplicationNotFound => new AuthProblemDescriptor(StatusCodes.Status404NotFound, NotFound, "Application not found"),
            AuthErrorCatalog.ApplicationInactive => new AuthProblemDescriptor(StatusCodes.Status403Forbidden, Forbidden, "Application is inactive"),
            AuthErrorCatalog.PermissionCodeNotFound => new AuthProblemDescriptor(StatusCodes.Status400BadRequest, BusinessRuleViolation, "Permission code not found"),
            AuthErrorCatalog.SystemWorkspaceImportForbidden => new AuthProblemDescriptor(StatusCodes.Status400BadRequest, BusinessRuleViolation, "Cannot import system workspaces"),
            AuthErrorCatalog.InternalAuthDisabled => new AuthProblemDescriptor(StatusCodes.Status403Forbidden, Forbidden, "Internal authentication is disabled for this user"),
            AuthErrorCatalog.ConsentRequired => new AuthProblemDescriptor(StatusCodes.Status403Forbidden, "Consent required", "User consent is required"),
            AuthErrorCatalog.AuthorizationNotFound => new AuthProblemDescriptor(StatusCodes.Status404NotFound, NotFound, "Authorization not found"),
            AuthErrorCatalog.InvalidRedirectUri => new AuthProblemDescriptor(StatusCodes.Status400BadRequest, ValidationError, "Invalid redirect URI"),
            VerificationErrorCatalog.InvalidChallenge => new AuthProblemDescriptor(StatusCodes.Status404NotFound, NotFound, "Verification challenge not found"),
            VerificationErrorCatalog.ChallengeExpired => new AuthProblemDescriptor(StatusCodes.Status410Gone, "Challenge expired", "Verification challenge expired"),
            VerificationErrorCatalog.InvalidOtp => new AuthProblemDescriptor(StatusCodes.Status401Unauthorized, Unauthorized, "Verification code is invalid"),
            VerificationErrorCatalog.MaxAttemptsExceeded => new AuthProblemDescriptor(StatusCodes.Status429TooManyRequests, "Too many attempts", "Maximum verification attempts exceeded"),
            VerificationErrorCatalog.VerificationCooldown => new AuthProblemDescriptor(StatusCodes.Status429TooManyRequests, "Too many requests", "Verification already in progress"),
            VerificationErrorCatalog.UserNotFound => new AuthProblemDescriptor(StatusCodes.Status404NotFound, NotFound, "User not found"),
            VerificationErrorCatalog.NoEmailConfigured => new AuthProblemDescriptor(StatusCodes.Status400BadRequest, ValidationError, "No email configured for user"),
            VerificationErrorCatalog.NoPhoneConfigured => new AuthProblemDescriptor(StatusCodes.Status400BadRequest, ValidationError, "No phone configured for user"),
            AuthErrorCatalog.SessionNotFound => new AuthProblemDescriptor(StatusCodes.Status404NotFound, NotFound, "Session not found"),
            AuthErrorCatalog.SessionAlreadyRevoked => new AuthProblemDescriptor(StatusCodes.Status409Conflict, Conflict, "Session is already revoked"),
            AuthErrorCatalog.SessionRevoked => new AuthProblemDescriptor(StatusCodes.Status403Forbidden, Forbidden, "Session has been revoked"),
            AuthErrorCatalog.AccountLockedOut => new AuthProblemDescriptor(StatusCodes.Status403Forbidden, Forbidden, "Account is temporarily locked due to too many failed login attempts"),
            _ => new AuthProblemDescriptor(StatusCodes.Status400BadRequest, BusinessRuleViolation, BusinessRuleViolation)
        };
    }
}

public sealed record AuthProblemDescriptor(int StatusCode, string Title, string Detail);
