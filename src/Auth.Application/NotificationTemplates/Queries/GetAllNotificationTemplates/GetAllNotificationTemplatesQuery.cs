using MediatR;

namespace Auth.Application.NotificationTemplates.Queries.GetAllNotificationTemplates;

public sealed record GetAllNotificationTemplatesQuery() : IRequest<IReadOnlyCollection<NotificationTemplateDto>>;
