namespace Auth.Domain;

public sealed class NotificationTemplate : EntityBase
{
    [Auditable] public NotificationTemplateType Type { get; set; }
    [Auditable] public string Locale { get; set; } = "en-US";
    [Auditable] public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
