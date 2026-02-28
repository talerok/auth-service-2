namespace Auth.Domain;

public sealed class NotificationTemplate : EntityBase
{
    public TwoFactorChannel Channel { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
