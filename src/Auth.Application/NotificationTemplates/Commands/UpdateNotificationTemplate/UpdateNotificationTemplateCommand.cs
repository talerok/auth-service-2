using MediatR;

namespace Auth.Application.NotificationTemplates.Commands.UpdateNotificationTemplate;

public sealed record UpdateNotificationTemplateCommand(string Channel, string Subject, string Body) : IRequest<NotificationTemplateDto?>;
