namespace Auth.Domain;

public sealed class PasswordChangeChallenge
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UsedAt { get; private set; }
    public User? User { get; private set; }

    private PasswordChangeChallenge() { }

    public static PasswordChangeChallenge Create(Guid userId, DateTime expiresAt)
    {
        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentOutOfRangeException(nameof(expiresAt));

        return new PasswordChangeChallenge
        {
            UserId = userId,
            ExpiresAt = expiresAt
        };
    }

    public bool IsExpired(DateTime utcNow) => ExpiresAt <= utcNow;

    public void MarkAsUsed()
    {
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
    }
}
