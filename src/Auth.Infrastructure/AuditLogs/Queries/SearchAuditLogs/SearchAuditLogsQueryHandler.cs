using Auth.Application;
using Auth.Application.AuditLogs.Queries.SearchAuditLogs;
using MediatR;

namespace Auth.Infrastructure.AuditLogs.Queries.SearchAuditLogs;

internal sealed class SearchAuditLogsQueryHandler(
    ISearchService searchService) : IRequestHandler<SearchAuditLogsQuery, SearchResponse<AuditLogDto>>
{
    public Task<SearchResponse<AuditLogDto>> Handle(SearchAuditLogsQuery query, CancellationToken cancellationToken) =>
        searchService.SearchAuditLogsAsync(query.Request, cancellationToken);
}
