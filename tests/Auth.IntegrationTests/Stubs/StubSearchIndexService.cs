namespace Auth.IntegrationTests.Stubs;

internal sealed class StubSearchIndexService : ISearchIndexService
{
    public Task IndexUserAsync(UserDto user, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeleteUserAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task IndexRoleAsync(RoleDto role, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeleteRoleAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task IndexPermissionAsync(PermissionDto permission, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeletePermissionAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task IndexWorkspaceAsync(WorkspaceDto workspace, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeleteWorkspaceAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task IndexApplicationAsync(ApplicationDto application, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeleteApplicationAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task IndexServiceAccountAsync(ServiceAccountDto serviceAccount, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeleteServiceAccountAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task BulkIndexUsersAsync(IReadOnlyCollection<UserDto> users, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task BulkIndexRolesAsync(IReadOnlyCollection<RoleDto> roles, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task BulkIndexPermissionsAsync(IReadOnlyCollection<PermissionDto> permissions, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task BulkIndexWorkspacesAsync(IReadOnlyCollection<WorkspaceDto> workspaces, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task BulkIndexApplicationsAsync(IReadOnlyCollection<ApplicationDto> applications, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task BulkIndexServiceAccountsAsync(IReadOnlyCollection<ServiceAccountDto> serviceAccounts, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task IndexAuditLogAsync(AuditLogDto auditLog, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task BulkIndexAuditLogsAsync(IReadOnlyCollection<AuditLogDto> auditLogs, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task IndexNotificationTemplateAsync(NotificationTemplateDto template, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeleteNotificationTemplateAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task BulkIndexNotificationTemplatesAsync(IReadOnlyCollection<NotificationTemplateDto> templates, CancellationToken cancellationToken) => Task.CompletedTask;
}
