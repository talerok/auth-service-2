namespace Auth.Domain;

public sealed class User : EntityBase
{
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsInternalAuthEnabled { get; set; } = true;
    public bool MustChangePassword { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public TwoFactorChannel? TwoFactorChannel { get; private set; }
    public ICollection<UserWorkspace> UserWorkspaces { get; private set; } = [];
public ICollection<TwoFactorChallenge> TwoFactorChallenges { get; private set; } = [];

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
}
