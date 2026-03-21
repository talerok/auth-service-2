namespace Auth.Application;

public interface ISearchService
{
    Task<SearchResponse<UserDto>> SearchUsersAsync(SearchRequest request, CancellationToken cancellationToken);
    Task<SearchResponse<RoleDto>> SearchRolesAsync(SearchRequest request, CancellationToken cancellationToken);
    Task<SearchResponse<PermissionDto>> SearchPermissionsAsync(SearchRequest request, CancellationToken cancellationToken);
    Task<SearchResponse<WorkspaceDto>> SearchWorkspacesAsync(SearchRequest request, CancellationToken cancellationToken);
    Task<SearchResponse<ApiClientDto>> SearchApiClientsAsync(SearchRequest request, CancellationToken cancellationToken);
}

public interface ISearchIndexService
{
    Task IndexUserAsync(UserDto user, CancellationToken cancellationToken);
    Task DeleteUserAsync(Guid id, CancellationToken cancellationToken);

    Task IndexRoleAsync(RoleDto role, CancellationToken cancellationToken);
    Task DeleteRoleAsync(Guid id, CancellationToken cancellationToken);

    Task IndexPermissionAsync(PermissionDto permission, CancellationToken cancellationToken);
    Task DeletePermissionAsync(Guid id, CancellationToken cancellationToken);

    Task IndexWorkspaceAsync(WorkspaceDto workspace, CancellationToken cancellationToken);
    Task DeleteWorkspaceAsync(Guid id, CancellationToken cancellationToken);

    Task IndexApiClientAsync(ApiClientDto apiClient, CancellationToken cancellationToken);
    Task DeleteApiClientAsync(Guid id, CancellationToken cancellationToken);

    Task BulkIndexUsersAsync(IReadOnlyCollection<UserDto> users, CancellationToken cancellationToken);
    Task BulkIndexRolesAsync(IReadOnlyCollection<RoleDto> roles, CancellationToken cancellationToken);
    Task BulkIndexPermissionsAsync(IReadOnlyCollection<PermissionDto> permissions, CancellationToken cancellationToken);
    Task BulkIndexWorkspacesAsync(IReadOnlyCollection<WorkspaceDto> workspaces, CancellationToken cancellationToken);
    Task BulkIndexApiClientsAsync(IReadOnlyCollection<ApiClientDto> apiClients, CancellationToken cancellationToken);
}

public interface ISearchMaintenanceService
{
    Task EnsureIndicesAsync(CancellationToken cancellationToken);
    Task ReindexAllAsync(CancellationToken cancellationToken);
    Task ReindexUsersAsync(CancellationToken cancellationToken);
    Task ReindexRolesAsync(CancellationToken cancellationToken);
    Task ReindexPermissionsAsync(CancellationToken cancellationToken);
    Task ReindexWorkspacesAsync(CancellationToken cancellationToken);
    Task ReindexApiClientsAsync(CancellationToken cancellationToken);
}
