using Auth.Application;
using Auth.Infrastructure;
using Microsoft.EntityFrameworkCore;
using OpenSearch.Client;

namespace Auth.Infrastructure.Integration.Search;

public sealed class OpenSearchMaintenanceService(
    IOpenSearchClient client,
    AuthDbContext dbContext,
    OpenSearchIndexNames indexNames,
    ISearchIndexService searchIndexService) : ISearchMaintenanceService
{
    public async Task EnsureIndicesAsync(CancellationToken cancellationToken)
    {
        if (await EnsureIndexExistsAsync<UserDto>(indexNames.Users, p => p
                .Keyword(k => k.Name(n => n.Id))
                .Keyword(k => k.Name(n => n.Username))
                .Keyword(k => k.Name(n => n.FullName))
                .Keyword(k => k.Name(n => n.Email))
                .Boolean(b => b.Name(n => n.IsActive))
                .Boolean(b => b.Name(n => n.MustChangePassword)), cancellationToken))
        {
            await ReindexUsersAsync(cancellationToken);
        }

        if (await EnsureIndexExistsAsync<RoleDto>(indexNames.Roles, p => p
                .Keyword(k => k.Name(n => n.Id))
                .Keyword(k => k.Name(n => n.Name))
                .Keyword(k => k.Name(n => n.Description)), cancellationToken))
        {
            await ReindexRolesAsync(cancellationToken);
        }

        if (await EnsureIndexExistsAsync<PermissionDto>(indexNames.Permissions, p => p
                .Keyword(k => k.Name(n => n.Id))
                .Number(n => n.Name(x => x.Bit).Type(NumberType.Integer))
                .Keyword(k => k.Name(n => n.Code))
                .Keyword(k => k.Name(n => n.Description))
                .Boolean(b => b.Name(n => n.IsSystem)), cancellationToken))
        {
            await ReindexPermissionsAsync(cancellationToken);
        }

        if (await EnsureIndexExistsAsync<WorkspaceDto>(indexNames.Workspaces, p => p
                .Keyword(k => k.Name(n => n.Id))
                .Keyword(k => k.Name(n => n.Name))
                .Keyword(k => k.Name(n => n.Code))
                .Keyword(k => k.Name(n => n.Description))
                .Boolean(b => b.Name(n => n.IsSystem)), cancellationToken))
        {
            await ReindexWorkspacesAsync(cancellationToken);
        }

        if (await EnsureIndexExistsAsync<ApiClientDto>(indexNames.ApiClients, p => p
                .Keyword(k => k.Name(n => n.Id))
                .Keyword(k => k.Name(n => n.Name))
                .Keyword(k => k.Name(n => n.Description))
                .Keyword(k => k.Name(n => n.ClientId))
                .Boolean(b => b.Name(n => n.IsActive)), cancellationToken))
        {
            await ReindexApiClientsAsync(cancellationToken);
        }
    }

    public async Task ReindexAllAsync(CancellationToken cancellationToken)
    {
        await ReindexUsersAsync(cancellationToken);
        await ReindexRolesAsync(cancellationToken);
        await ReindexPermissionsAsync(cancellationToken);
        await ReindexWorkspacesAsync(cancellationToken);
        await ReindexApiClientsAsync(cancellationToken);
    }

    public async Task ReindexUsersAsync(CancellationToken cancellationToken)
    {
        var users = await dbContext.Users.AsNoTracking()
            .Select(x => new UserDto(x.Id, x.Username, x.FullName, x.Email, x.Phone, x.IsActive, x.IsInternalAuthEnabled, x.MustChangePassword, x.TwoFactorEnabled, x.TwoFactorChannel))
            .ToListAsync(cancellationToken);
        foreach (var user in users)
        {
            await searchIndexService.IndexUserAsync(user, cancellationToken);
        }
    }

    public async Task ReindexRolesAsync(CancellationToken cancellationToken)
    {
        var roles = await dbContext.Roles.AsNoTracking()
            .Select(x => new RoleDto(x.Id, x.Name, x.Code, x.Description))
            .ToListAsync(cancellationToken);
        foreach (var role in roles)
        {
            await searchIndexService.IndexRoleAsync(role, cancellationToken);
        }
    }

    public async Task ReindexPermissionsAsync(CancellationToken cancellationToken)
    {
        var permissions = await dbContext.Permissions.AsNoTracking()
            .Select(x => new PermissionDto(x.Id, x.Bit, x.Code, x.Description, x.IsSystem))
            .ToListAsync(cancellationToken);
        foreach (var permission in permissions)
        {
            await searchIndexService.IndexPermissionAsync(permission, cancellationToken);
        }
    }

    public async Task ReindexWorkspacesAsync(CancellationToken cancellationToken)
    {
        var workspaces = await dbContext.Workspaces.AsNoTracking()
            .Select(x => new WorkspaceDto(x.Id, x.Name, x.Code, x.Description, x.IsSystem))
            .ToListAsync(cancellationToken);
        foreach (var workspace in workspaces)
        {
            await searchIndexService.IndexWorkspaceAsync(workspace, cancellationToken);
        }
    }

    public async Task ReindexApiClientsAsync(CancellationToken cancellationToken)
    {
        var apiClients = await dbContext.ApiClients.AsNoTracking()
            .Select(x => new ApiClientDto(x.Id, x.Name, x.Description, x.ClientId, x.IsActive))
            .ToListAsync(cancellationToken);
        foreach (var apiClient in apiClients)
        {
            await searchIndexService.IndexApiClientAsync(apiClient, cancellationToken);
        }
    }

    // Returns true if the index was created (did not exist before)
    private async Task<bool> EnsureIndexExistsAsync<TDocument>(
        string indexName,
        Func<PropertiesDescriptor<TDocument>, IPromise<IProperties>> propertiesSelector,
        CancellationToken cancellationToken) where TDocument : class
    {
        var existsResponse = await client.Indices.ExistsAsync(indexName, ct: cancellationToken);
        if (existsResponse.Exists)
        {
            var putMappingResponse = await client.Indices.PutMappingAsync<TDocument>(
                p => p.Index(indexName).Properties(propertiesSelector), cancellationToken);

            if (!putMappingResponse.IsValid)
            {
                throw new InvalidOperationException(putMappingResponse.DebugInformation);
            }

            return false;
        }

        var createResponse = await client.Indices.CreateAsync(indexName,
            c => c.Map(m => m.Properties(propertiesSelector)), cancellationToken);

        if (!createResponse.IsValid)
        {
            throw new InvalidOperationException(createResponse.DebugInformation);
        }

        return true;
    }
}
