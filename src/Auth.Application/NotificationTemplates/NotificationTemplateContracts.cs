namespace Auth.Application;

public sealed record NotificationTemplateDto(Guid Id, string Channel, string Subject, string HtmlBody, string TextBody);

public sealed record UpdateNotificationTemplateRequest(string Subject, string HtmlBody, string TextBody);
