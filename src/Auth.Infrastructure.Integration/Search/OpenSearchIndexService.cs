using Auth.Application;
using OpenSearch.Client;
using OpenSearch.Net;

namespace Auth.Infrastructure.Integration.Search;

public sealed class OpenSearchIndexService(
    IOpenSearchClient client,
    OpenSearchIndexNames indexNames,
    OpenSearchRetryExecutor retryExecutor) : ISearchIndexService
{
    public Task IndexUserAsync(UserDto user, CancellationToken cancellationToken) =>
        IndexAsync(indexNames.Users, user.Id, user, cancellationToken);

    public Task DeleteUserAsync(Guid id, CancellationToken cancellationToken) =>
        DeleteAsync(indexNames.Users, id, cancellationToken);

    public Task IndexRoleAsync(RoleDto role, CancellationToken cancellationToken) =>
        IndexAsync(indexNames.Roles, role.Id, role, cancellationToken);

    public Task DeleteRoleAsync(Guid id, CancellationToken cancellationToken) =>
        DeleteAsync(indexNames.Roles, id, cancellationToken);

    public Task IndexPermissionAsync(PermissionDto permission, CancellationToken cancellationToken) =>
        IndexAsync(indexNames.Permissions, permission.Id, permission, cancellationToken);

    public Task DeletePermissionAsync(Guid id, CancellationToken cancellationToken) =>
        DeleteAsync(indexNames.Permissions, id, cancellationToken);

    public Task IndexWorkspaceAsync(WorkspaceDto workspace, CancellationToken cancellationToken) =>
        IndexAsync(indexNames.Workspaces, workspace.Id, workspace, cancellationToken);

    public Task DeleteWorkspaceAsync(Guid id, CancellationToken cancellationToken) =>
        DeleteAsync(indexNames.Workspaces, id, cancellationToken);

    private async Task IndexAsync<TDocument>(string indexName, Guid id, TDocument document, CancellationToken cancellationToken)
        where TDocument : class
    {
        await retryExecutor.ExecuteAsync(
            async () =>
            {
                var response = await client.IndexAsync(document, i => i.Index(indexName).Id(id).Refresh(Refresh.WaitFor), cancellationToken);
                if (!response.IsValid)
                {
                    throw new InvalidOperationException(response.DebugInformation);
                }
            },
            $"index {typeof(TDocument).Name} document {id} into {indexName}",
            cancellationToken);
    }

    private async Task DeleteAsync(string indexName, Guid id, CancellationToken cancellationToken)
    {
        await retryExecutor.ExecuteAsync(
            async () =>
            {
                var response = await client.DeleteAsync<object>(id.ToString("D"), d => d.Index(indexName).Refresh(Refresh.WaitFor), cancellationToken);
                if (!response.IsValid)
                {
                    throw new InvalidOperationException(response.DebugInformation);
                }
            },
            $"delete document {id} from {indexName}",
            cancellationToken);
    }
}
