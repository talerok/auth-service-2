namespace Auth.Application;

public sealed record NotificationTemplateDto(Guid Id, string Channel, string Subject, string Body);

public sealed record UpdateNotificationTemplateRequest(string Subject, string Body);
