using MediatR;

namespace Auth.Application.NotificationTemplates.Queries.GetNotificationTemplateById;

public sealed record GetNotificationTemplateByIdQuery(Guid Id) : IRequest<NotificationTemplateDto?>;
