using Auth.Application;
using OpenSearch.Client;
using AppSearchRequest = Auth.Application.SearchRequest;

namespace Auth.Infrastructure.Integration.Search;

public sealed class OpenSearchQueryService(
    IOpenSearchClient client,
    OpenSearchIndexNames indexNames,
    OpenSearchRetryExecutor retryExecutor) : ISearchService
{
    public Task<Auth.Application.SearchResponse<UserDto>> SearchUsersAsync(AppSearchRequest request, CancellationToken cancellationToken) =>
        SearchAsync<UserDto>(indexNames.Users, request, cancellationToken);

    public Task<Auth.Application.SearchResponse<RoleDto>> SearchRolesAsync(AppSearchRequest request, CancellationToken cancellationToken) =>
        SearchAsync<RoleDto>(indexNames.Roles, request, cancellationToken);

    public Task<Auth.Application.SearchResponse<PermissionDto>> SearchPermissionsAsync(AppSearchRequest request, CancellationToken cancellationToken) =>
        SearchAsync<PermissionDto>(indexNames.Permissions, request, cancellationToken);

    public Task<Auth.Application.SearchResponse<WorkspaceDto>> SearchWorkspacesAsync(AppSearchRequest request, CancellationToken cancellationToken) =>
        SearchAsync<WorkspaceDto>(indexNames.Workspaces, request, cancellationToken);

    public Task<Auth.Application.SearchResponse<ApplicationDto>> SearchApplicationsAsync(AppSearchRequest request, CancellationToken cancellationToken) =>
        SearchAsync<ApplicationDto>(indexNames.Applications, request, cancellationToken);

    public Task<Auth.Application.SearchResponse<ServiceAccountDto>> SearchServiceAccountsAsync(AppSearchRequest request, CancellationToken cancellationToken) =>
        SearchAsync<ServiceAccountDto>(indexNames.ServiceAccounts, request, cancellationToken);

    public Task<Auth.Application.SearchResponse<AuditLogDto>> SearchAuditLogsAsync(AppSearchRequest request, CancellationToken cancellationToken) =>
        SearchAsync<AuditLogDto>(indexNames.AuditLogs, request, cancellationToken);

    private async Task<Auth.Application.SearchResponse<TDocument>> SearchAsync<TDocument>(string indexName, AppSearchRequest request, CancellationToken cancellationToken)
        where TDocument : class
    {
        var queryString = BuildQueryString(request);

        return await retryExecutor.ExecuteAsync(
            async () =>
            {
                var response = await client.SearchAsync<TDocument>(s => s
                    .Index(indexName)
                    .From((request.Page - 1) * request.PageSize)
                    .Size(request.PageSize)
                    .Query(q => q.QueryString(x => x.Query(queryString)))
                    .Sort(x => ApplySort(x, request)),
                    cancellationToken);

                if (!response.IsValid)
                {
                    throw new InvalidOperationException(response.DebugInformation);
                }

                var items = response.Documents.ToArray();
                return new Auth.Application.SearchResponse<TDocument>(items, request.Page, request.PageSize, response.Total);
            },
            $"search in {indexName}",
            _ => new Auth.Application.SearchResponse<TDocument>(Array.Empty<TDocument>(), request.Page, request.PageSize, 0),
            cancellationToken);
    }

    private static string BuildQueryString(AppSearchRequest request)
    {
        if (request.Filter is null || request.Filter.Count == 0)
        {
            return "*";
        }

        var clauses = request.Filter
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .SelectMany(x => BuildFieldClauses(x.Key, x.Value))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        return clauses.Length == 0 ? "*" : string.Join(" AND ", clauses);
    }

    private static IEnumerable<string> BuildFieldClauses(string fieldName, SearchFieldFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Eq))
        {
            yield return $"{fieldName}:\"{EscapeQueryValue(filter.Eq)}\"";
        }

        if (filter.In is not null)
        {
            var values = filter.In
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => $"\"{EscapeQueryValue(x)}\"")
                .ToArray();

            if (values.Length > 0)
            {
                yield return $"{fieldName}:({string.Join(" OR ", values)})";
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.Ts))
        {
            yield return $"{fieldName}:*{EscapeQueryValue(filter.Ts)}*";
        }

        if (!string.IsNullOrWhiteSpace(filter.From) || !string.IsNullOrWhiteSpace(filter.To))
        {
            var from = string.IsNullOrWhiteSpace(filter.From) ? "*" : EscapeQueryValue(filter.From);
            var to = string.IsNullOrWhiteSpace(filter.To) ? "*" : EscapeQueryValue(filter.To);
            yield return $"{fieldName}:[{from} TO {to}]";
        }
    }

    private static SortDescriptor<TDocument> ApplySort<TDocument>(SortDescriptor<TDocument> sortDescriptor, AppSearchRequest request)
        where TDocument : class
    {
        if (string.IsNullOrWhiteSpace(request.SortBy))
        {
            return sortDescriptor;
        }

        return request.SortOrder == SearchSortOrder.Desc
            ? sortDescriptor.Descending(request.SortBy)
            : sortDescriptor.Ascending(request.SortBy);
    }

    private static string EscapeQueryValue(string value) =>
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
}
