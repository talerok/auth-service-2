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

    public Task IndexApplicationAsync(ApplicationDto application, CancellationToken cancellationToken) =>
        IndexAsync(indexNames.Applications, application.Id, application, cancellationToken);

    public Task DeleteApplicationAsync(Guid id, CancellationToken cancellationToken) =>
        DeleteAsync(indexNames.Applications, id, cancellationToken);

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

    private async Task BulkIndexAsync<TDocument>(string indexName, IReadOnlyCollection<TDocument> documents, Func<TDocument, Guid> idSelector, CancellationToken cancellationToken)
        where TDocument : class
    {
        if (documents.Count == 0) return;

        int bulkSize = 5000;
        foreach (var batch in documents.Chunk(bulkSize))
        {
            await retryExecutor.ExecuteAsync(
                async () =>
                {
                    var response = await client.BulkAsync(b => b
                        .Index(indexName)
                        .IndexMany(batch, (descriptor, doc) => descriptor.Id(idSelector(doc))), cancellationToken);
                    if (response.Errors)
                    {
                        var firstError = response.ItemsWithErrors.First();
                        throw new InvalidOperationException($"Bulk index failed: {firstError.Error.Reason}");
                    }
                },
                $"bulk index {batch.Length} {typeof(TDocument).Name} documents into {indexName}",
                cancellationToken);
        }

        await client.Indices.RefreshAsync(indexName, ct: cancellationToken);
    }

    public Task BulkIndexUsersAsync(IReadOnlyCollection<UserDto> users, CancellationToken cancellationToken) =>
        BulkIndexAsync(indexNames.Users, users, x => x.Id, cancellationToken);

    public Task BulkIndexRolesAsync(IReadOnlyCollection<RoleDto> roles, CancellationToken cancellationToken) =>
        BulkIndexAsync(indexNames.Roles, roles, x => x.Id, cancellationToken);

    public Task BulkIndexPermissionsAsync(IReadOnlyCollection<PermissionDto> permissions, CancellationToken cancellationToken) =>
        BulkIndexAsync(indexNames.Permissions, permissions, x => x.Id, cancellationToken);

    public Task BulkIndexWorkspacesAsync(IReadOnlyCollection<WorkspaceDto> workspaces, CancellationToken cancellationToken) =>
        BulkIndexAsync(indexNames.Workspaces, workspaces, x => x.Id, cancellationToken);

    public Task BulkIndexApplicationsAsync(IReadOnlyCollection<ApplicationDto> applications, CancellationToken cancellationToken) =>
        BulkIndexAsync(indexNames.Applications, applications, x => x.Id, cancellationToken);

    public Task IndexServiceAccountAsync(ServiceAccountDto serviceAccount, CancellationToken cancellationToken) =>
        IndexAsync(indexNames.ServiceAccounts, serviceAccount.Id, serviceAccount, cancellationToken);

    public Task DeleteServiceAccountAsync(Guid id, CancellationToken cancellationToken) =>
        DeleteAsync(indexNames.ServiceAccounts, id, cancellationToken);

    public Task BulkIndexServiceAccountsAsync(IReadOnlyCollection<ServiceAccountDto> serviceAccounts, CancellationToken cancellationToken) =>
        BulkIndexAsync(indexNames.ServiceAccounts, serviceAccounts, x => x.Id, cancellationToken);

    public Task IndexAuditLogAsync(AuditLogDto entry, CancellationToken cancellationToken) =>
        IndexAsync(indexNames.AuditLogs, entry.Id, entry, cancellationToken);

    public Task BulkIndexAuditLogsAsync(IReadOnlyCollection<AuditLogDto> entries, CancellationToken cancellationToken) =>
        BulkIndexAsync(indexNames.AuditLogs, entries, x => x.Id, cancellationToken);

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
