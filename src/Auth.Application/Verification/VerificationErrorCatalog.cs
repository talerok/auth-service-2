namespace Auth.Application.Verification;

public static class VerificationErrorCatalog
{
    public const string VerificationCooldown = "VERIFICATION_COOLDOWN";
    public const string InvalidChallenge = "INVALID_CHALLENGE";
    public const string ChallengeExpired = "CHALLENGE_EXPIRED";
    public const string InvalidOtp = "INVALID_OTP";
    public const string MaxAttemptsExceeded = "MAX_ATTEMPTS_EXCEEDED";
    public const string UserNotFound = "USER_NOT_FOUND";
    public const string NoEmailConfigured = "NO_EMAIL_CONFIGURED";
    public const string NoPhoneConfigured = "NO_PHONE_CONFIGURED";
}
