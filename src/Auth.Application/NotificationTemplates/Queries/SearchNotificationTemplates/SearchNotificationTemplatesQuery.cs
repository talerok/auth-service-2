using MediatR;

namespace Auth.Application.NotificationTemplates.Queries.SearchNotificationTemplates;

public sealed record SearchNotificationTemplatesQuery(SearchRequest Request) : IRequest<SearchResponse<NotificationTemplateDto>>;
