namespace Auth.IntegrationTests.Stubs;

internal sealed class StubSearchService : ISearchService
{
    public Task<SearchResponse<UserDto>> SearchUsersAsync(SearchRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new SearchResponse<UserDto>([], request.Page, request.PageSize, 0));

    public Task<SearchResponse<RoleDto>> SearchRolesAsync(SearchRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new SearchResponse<RoleDto>([], request.Page, request.PageSize, 0));

    public Task<SearchResponse<PermissionDto>> SearchPermissionsAsync(SearchRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new SearchResponse<PermissionDto>([], request.Page, request.PageSize, 0));

    public Task<SearchResponse<WorkspaceDto>> SearchWorkspacesAsync(SearchRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new SearchResponse<WorkspaceDto>([], request.Page, request.PageSize, 0));

    public Task<SearchResponse<ApplicationDto>> SearchApplicationsAsync(SearchRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new SearchResponse<ApplicationDto>([], request.Page, request.PageSize, 0));

    public Task<SearchResponse<ServiceAccountDto>> SearchServiceAccountsAsync(SearchRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new SearchResponse<ServiceAccountDto>([], request.Page, request.PageSize, 0));

    public Task<SearchResponse<AuditLogDto>> SearchAuditLogsAsync(SearchRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new SearchResponse<AuditLogDto>([], request.Page, request.PageSize, 0));
}
