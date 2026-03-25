using MediatR;

namespace Auth.Application.AuditLogs.Queries.SearchAuditLogs;

public sealed record SearchAuditLogsQuery(SearchRequest Request) : IRequest<SearchResponse<AuditLogDto>>;
