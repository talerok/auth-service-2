namespace Auth.Domain;

public sealed class User : EntityBase
{
    [Auditable] public string Username { get; set; } = string.Empty;
    [Auditable] public string FullName { get; set; } = string.Empty;
    [Auditable] public string Email { get; set; } = string.Empty;
    [Auditable] public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    [Auditable] public bool IsActive { get; set; } = true;
    [Auditable] public bool IsInternalAuthEnabled { get; set; } = true;
    [Auditable] public bool MustChangePassword { get; private set; }
    [Auditable] public bool TwoFactorEnabled { get; private set; }
    [Auditable] public TwoFactorChannel? TwoFactorChannel { get; private set; }
    [Auditable] public string Locale { get; set; } = "en-US";
    [Auditable] public bool EmailVerified { get; set; }
    [Auditable] public bool PhoneVerified { get; set; }
    [Auditable] public int? PasswordMaxAgeDays { get; set; }
    public DateTime? PasswordChangedAt { get; private set; }
    public ICollection<UserWorkspace> UserWorkspaces { get; private set; } = [];
    public ICollection<TwoFactorChallenge> TwoFactorChallenges { get; private set; } = [];

    public void SetPassword(string hash)
    {
        PasswordHash = hash;
        PasswordChangedAt = DateTime.UtcNow;
    }

    public bool IsPasswordExpired(int defaultMaxAgeDays)
    {
        var effectiveMaxAge = PasswordMaxAgeDays ?? defaultMaxAgeDays;
        if (effectiveMaxAge <= 0)
            return false;
        if (PasswordChangedAt is null)
            return false;
        return DateTime.UtcNow > PasswordChangedAt.Value.AddDays(effectiveMaxAge);
    }

    public long? GetPasswordExpirationUnixTimestamp(int defaultMaxAgeDays)
    {
        var effectiveMaxAge = PasswordMaxAgeDays ?? defaultMaxAgeDays;
        if (effectiveMaxAge <= 0)
            return null;
        if (PasswordChangedAt is null)
            return null;
        var expiresAt = PasswordChangedAt.Value.AddDays(effectiveMaxAge);
        return new DateTimeOffset(expiresAt, TimeSpan.Zero).ToUnixTimeSeconds();
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void MarkMustChangePassword()
    {
        MustChangePassword = true;
    }

    public void ClearMustChangePassword()
    {
        MustChangePassword = false;
    }

    public void EnableTwoFactor(TwoFactorChannel channel)
    {
        TwoFactorEnabled = true;
        TwoFactorChannel = channel;
    }

    public void DisableTwoFactor()
    {
        TwoFactorEnabled = false;
        TwoFactorChannel = null;
    }

    public void VerifyEmail() => EmailVerified = true;
    public void VerifyPhone() => PhoneVerified = true;

}
