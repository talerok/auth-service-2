namespace Auth.Domain;

public enum NotificationTemplateType
{
    TwoFactorEmail = 1,
    TwoFactorSms = 2,
    EmailVerification = 3,
    PhoneVerification = 4
}
