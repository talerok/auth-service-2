using Auth.Application.Sessions;

namespace Auth.Application;

public interface ISearchService
{
    Task<SearchResponse<UserDto>> SearchUsersAsync(SearchRequest request, CancellationToken cancellationToken);
    Task<SearchResponse<RoleDto>> SearchRolesAsync(SearchRequest request, CancellationToken cancellationToken);
    Task<SearchResponse<PermissionDto>> SearchPermissionsAsync(SearchRequest request, CancellationToken cancellationToken);
    Task<SearchResponse<WorkspaceDto>> SearchWorkspacesAsync(SearchRequest request, CancellationToken cancellationToken);
    Task<SearchResponse<ApplicationDto>> SearchApplicationsAsync(SearchRequest request, CancellationToken cancellationToken);
    Task<SearchResponse<ServiceAccountDto>> SearchServiceAccountsAsync(SearchRequest request, CancellationToken cancellationToken);
    Task<SearchResponse<AuditLogDto>> SearchAuditLogsAsync(SearchRequest request, CancellationToken cancellationToken);
    Task<SearchResponse<NotificationTemplateDto>> SearchNotificationTemplatesAsync(SearchRequest request, CancellationToken cancellationToken);
    Task<SearchResponse<UserSessionSearchDto>> SearchSessionsAsync(SearchRequest request, CancellationToken cancellationToken);
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

    Task IndexApplicationAsync(ApplicationDto application, CancellationToken cancellationToken);
    Task DeleteApplicationAsync(Guid id, CancellationToken cancellationToken);

    Task IndexServiceAccountAsync(ServiceAccountDto serviceAccount, CancellationToken cancellationToken);
    Task DeleteServiceAccountAsync(Guid id, CancellationToken cancellationToken);

    Task BulkIndexUsersAsync(IReadOnlyCollection<UserDto> users, CancellationToken cancellationToken);
    Task BulkIndexRolesAsync(IReadOnlyCollection<RoleDto> roles, CancellationToken cancellationToken);
    Task BulkIndexPermissionsAsync(IReadOnlyCollection<PermissionDto> permissions, CancellationToken cancellationToken);
    Task BulkIndexWorkspacesAsync(IReadOnlyCollection<WorkspaceDto> workspaces, CancellationToken cancellationToken);
    Task BulkIndexApplicationsAsync(IReadOnlyCollection<ApplicationDto> applications, CancellationToken cancellationToken);
    Task BulkIndexServiceAccountsAsync(IReadOnlyCollection<ServiceAccountDto> serviceAccounts, CancellationToken cancellationToken);

    Task IndexAuditLogAsync(AuditLogDto entry, CancellationToken cancellationToken);
    Task BulkIndexAuditLogsAsync(IReadOnlyCollection<AuditLogDto> entries, CancellationToken cancellationToken);

    Task IndexNotificationTemplateAsync(NotificationTemplateDto template, CancellationToken cancellationToken);
    Task DeleteNotificationTemplateAsync(Guid id, CancellationToken cancellationToken);
    Task BulkIndexNotificationTemplatesAsync(IReadOnlyCollection<NotificationTemplateDto> templates, CancellationToken cancellationToken);

    Task IndexSessionAsync(UserSessionSearchDto session, CancellationToken cancellationToken);
    Task DeleteSessionAsync(Guid id, CancellationToken cancellationToken);
    Task BulkIndexSessionsAsync(IReadOnlyCollection<UserSessionSearchDto> sessions, CancellationToken cancellationToken);
}

public interface ISearchMaintenanceService
{
    Task EnsureIndicesAsync(CancellationToken cancellationToken);
    Task ReindexAllAsync(CancellationToken cancellationToken);
    Task ReindexUsersAsync(CancellationToken cancellationToken);
    Task ReindexRolesAsync(CancellationToken cancellationToken);
    Task ReindexPermissionsAsync(CancellationToken cancellationToken);
    Task ReindexWorkspacesAsync(CancellationToken cancellationToken);
    Task ReindexApplicationsAsync(CancellationToken cancellationToken);
    Task ReindexServiceAccountsAsync(CancellationToken cancellationToken);
    Task ReindexAuditLogsAsync(CancellationToken cancellationToken);
    Task ReindexNotificationTemplatesAsync(CancellationToken cancellationToken);
    Task ReindexSessionsAsync(CancellationToken cancellationToken);
}
