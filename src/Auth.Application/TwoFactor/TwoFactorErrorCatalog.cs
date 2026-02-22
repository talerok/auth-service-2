namespace Auth.Application;

public static class TwoFactorErrorCatalog
{
    public const string UnsupportedChannel = "TWO_FACTOR_UNSUPPORTED_CHANNEL";
    public const string VerificationFailed = "TWO_FACTOR_VERIFICATION_FAILED";
    public const string ActivationNotCompleted = "TWO_FACTOR_ACTIVATION_NOT_COMPLETED";
    public const string Required = "TWO_FACTOR_REQUIRED";
    public const string NotRequired = "TWO_FACTOR_NOT_REQUIRED";
    public const string DeliveryFailed = "TWO_FACTOR_DELIVERY_FAILED";
    public const string ProviderUnavailable = "TWO_FACTOR_PROVIDER_UNAVAILABLE";
    public const string AttemptsExceeded = "TWO_FACTOR_ATTEMPTS_EXCEEDED";
    public const string OtpAlreadyUsed = "TWO_FACTOR_OTP_ALREADY_USED";
    public const string ChallengeExpired = "TWO_FACTOR_CHALLENGE_EXPIRED";
    public const string ChallengeNotFound = "TWO_FACTOR_CHALLENGE_NOT_FOUND";
}
