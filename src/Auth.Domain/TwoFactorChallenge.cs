namespace Auth.Domain;

public sealed class TwoFactorChallenge
{
    public const string PurposeActivation = "ACTIVATION";
    public const string PurposeLogin = "LOGIN";
    public const string PurposeEmailVerification = "EMAIL_VERIFICATION";
    public const string PurposePhoneVerification = "PHONE_VERIFICATION";
    public const string DeliveryPending = "PENDING";
    public const string DeliveryDelivered = "DELIVERED";
    public const string DeliveryFailed = "DELIVERY_FAILED";
    public const string ProviderUnavailable = "PROVIDER_UNAVAILABLE";

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string Purpose { get; private set; } = string.Empty;
    public TwoFactorChannel Channel { get; private set; } = TwoFactorChannel.Email;
    public string OtpHash { get; private set; } = string.Empty;
    public string OtpSalt { get; private set; } = string.Empty;
    public string OtpEncrypted { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public int Attempts { get; private set; }
    public int MaxAttempts { get; private set; } = 5;
    public bool IsUsed { get; private set; }
    public string DeliveryStatus { get; private set; } = DeliveryPending;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; private set; }
    public User? User { get; private set; }

    private TwoFactorChallenge()
    {
    }

    public static TwoFactorChallenge Create(
        Guid userId,
        string purpose,
        TwoFactorChannel channel,
        string otpHash,
        string otpSalt,
        string otpEncrypted,
        DateTime expiresAt,
        int maxAttempts)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(otpHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(otpSalt);
        ArgumentException.ThrowIfNullOrWhiteSpace(otpEncrypted);
        if (maxAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        }

        return new TwoFactorChallenge
        {
            UserId = userId,
            Purpose = purpose,
            Channel = channel,
            OtpHash = otpHash,
            OtpSalt = otpSalt,
            OtpEncrypted = otpEncrypted,
            ExpiresAt = expiresAt,
            MaxAttempts = maxAttempts,
            DeliveryStatus = DeliveryPending,
            Attempts = 0,
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool IsExpired(DateTime utcNow) => ExpiresAt <= utcNow;
    public bool HasAttemptsRemaining() => Attempts < MaxAttempts;

    public void MarkDelivered() => DeliveryStatus = DeliveryDelivered;
    public void MarkDeliveryFailed() => DeliveryStatus = DeliveryFailed;
    public void MarkProviderUnavailable() => DeliveryStatus = ProviderUnavailable;

    public void RegisterFailedAttempt() => Attempts += 1;

    public void MarkVerified()
    {
        IsUsed = true;
        CompletedAt = DateTime.UtcNow;
    }
}
