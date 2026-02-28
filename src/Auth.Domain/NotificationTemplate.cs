namespace Auth.Domain;

public sealed class NotificationTemplate : EntityBase
{
    public TwoFactorChannel Channel { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string TextBody { get; set; } = string.Empty;
}
