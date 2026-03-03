using MediatR;

namespace Auth.Application.NotificationTemplates.Queries.GetNotificationTemplateByChannel;

public sealed record GetNotificationTemplateByChannelQuery(string Channel) : IRequest<NotificationTemplateDto?>;
