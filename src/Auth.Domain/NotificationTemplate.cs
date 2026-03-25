namespace Auth.Domain;

public sealed class NotificationTemplate : EntityBase
{
    [Auditable] public TwoFactorChannel Channel { get; set; }
    [Auditable] public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
