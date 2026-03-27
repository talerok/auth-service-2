using Auth.Application.Common;

namespace Auth.Application;

public sealed record NotificationTemplateDto(Guid Id, string Type, string Locale, string Subject, string Body);

public sealed record CreateNotificationTemplateRequest(string Type, string Locale, string Subject, string Body);

public sealed record UpdateNotificationTemplateRequest(string Type, string Locale, string Subject, string Body);

public sealed record PatchNotificationTemplateRequest(Optional<string> Type, Optional<string> Locale, Optional<string> Subject, Optional<string> Body);
