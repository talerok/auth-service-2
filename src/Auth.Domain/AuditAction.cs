namespace Auth.Domain;

public enum AuditAction
{
    Create, Update, Patch, SoftDelete,
    ResetPassword, SetWorkspaces, SetIdentitySourceLinks,
    SetPermissions, RegenerateSecret, Import,
    Login, MfaVerify, EnableTwoFactor,
    ConfirmTwoFactorActivation, DisableTwoFactor, PasswordChange
}
