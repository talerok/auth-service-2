using Auth.Application;
using Auth.Application.NotificationTemplates.Queries.SearchNotificationTemplates;
using MediatR;

namespace Auth.Infrastructure.NotificationTemplates.Queries.SearchNotificationTemplates;

internal sealed class SearchNotificationTemplatesQueryHandler(
    ISearchService searchService) : IRequestHandler<SearchNotificationTemplatesQuery, SearchResponse<NotificationTemplateDto>>
{
    public Task<SearchResponse<NotificationTemplateDto>> Handle(SearchNotificationTemplatesQuery query, CancellationToken cancellationToken) =>
        searchService.SearchNotificationTemplatesAsync(query.Request, cancellationToken);
}
