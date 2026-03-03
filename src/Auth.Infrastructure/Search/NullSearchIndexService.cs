using Auth.Application;

namespace Auth.Infrastructure;

public sealed class NullSearchIndexService : ISearchIndexService
{
    public Task IndexUserAsync(UserDto user, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task DeleteUserAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task IndexRoleAsync(RoleDto role, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task DeleteRoleAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task IndexPermissionAsync(PermissionDto permission, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task DeletePermissionAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task IndexWorkspaceAsync(WorkspaceDto workspace, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task DeleteWorkspaceAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task IndexApiClientAsync(ApiClientDto apiClient, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task DeleteApiClientAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
}
