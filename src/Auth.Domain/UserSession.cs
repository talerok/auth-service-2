namespace Auth.Domain;

public sealed class UserSession
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public Guid? ApplicationId { get; private set; }
    public Application? Application { get; private set; }
    public string IpAddress { get; private set; } = string.Empty;
    public string UserAgent { get; private set; } = string.Empty;
    public string AuthMethod { get; private set; } = string.Empty;
    public bool IsRevoked { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; internal set; }
    public DateTime LastActivityAt { get; private set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }

    private UserSession() { }

    public static UserSession Create(
        Guid userId, string ipAddress, string userAgent,
        Guid? applicationId, string authMethod, int refreshTokenLifetimeDays)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID must not be empty.", nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(ipAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(userAgent);
        ArgumentException.ThrowIfNullOrWhiteSpace(authMethod);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(refreshTokenLifetimeDays);

        return new UserSession
        {
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent.Length > 500 ? userAgent[..500] : userAgent,
            ApplicationId = applicationId,
            AuthMethod = authMethod,
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenLifetimeDays)
        };
    }

    public bool IsActive => !IsRevoked && ExpiresAt > DateTime.UtcNow;

    public void TouchActivity() => LastActivityAt = DateTime.UtcNow;

    public void Revoke(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (IsRevoked) return;
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevokedReason = reason.Length > 100 ? reason[..100] : reason;
    }
}
