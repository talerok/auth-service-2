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
                .Boolean(b => b.Name(n => n.IsInternalAuthEnabled))
                .Boolean(b => b.Name(n => n.MustChangePassword)), cancellationToken))
        {
            await ReindexUsersAsync(cancellationToken);
        }

        if (await EnsureIndexExistsAsync<RoleDto>(indexNames.Roles, p => p
                .Keyword(k => k.Name(n => n.Id))
                .Keyword(k => k.Name(n => n.Name))
                .Keyword(k => k.Name(n => n.Code))
                .Keyword(k => k.Name(n => n.Description)), cancellationToken))
        {
            await ReindexRolesAsync(cancellationToken);
        }

        if (await EnsureIndexExistsAsync<PermissionDto>(indexNames.Permissions, p => p
                .Keyword(k => k.Name(n => n.Id))
                .Keyword(k => k.Name(n => n.Domain))
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

        if (await EnsureIndexExistsAsync<ApplicationDto>(indexNames.Applications, p => p
                .Keyword(k => k.Name(n => n.Id))
                .Keyword(k => k.Name(n => n.Name))
                .Keyword(k => k.Name(n => n.Description))
                .Keyword(k => k.Name(n => n.ClientId))
                .Boolean(b => b.Name(n => n.IsActive)), cancellationToken))
        {
            await ReindexApplicationsAsync(cancellationToken);
        }

        if (await EnsureIndexExistsAsync<ServiceAccountDto>(indexNames.ServiceAccounts, p => p
                .Keyword(k => k.Name(n => n.Id))
                .Keyword(k => k.Name(n => n.Name))
                .Keyword(k => k.Name(n => n.Description))
                .Keyword(k => k.Name(n => n.ClientId))
                .Boolean(b => b.Name(n => n.IsActive))
                .Number(n => n.Name(x => x.AccessTokenLifetimeMinutes).Type(NumberType.Integer)), cancellationToken))
        {
            await ReindexServiceAccountsAsync(cancellationToken);
        }
    }

    public async Task ReindexAllAsync(CancellationToken cancellationToken)
    {
        await DeleteAllIndicesAsync(cancellationToken);
        await EnsureIndicesAsync(cancellationToken);
    }

    public async Task ReindexUsersAsync(CancellationToken cancellationToken)
    {
        await ClearIndexAsync(indexNames.Users, cancellationToken);
        var users = await dbContext.Users.AsNoTracking()
            .Select(x => new UserDto(x.Id, x.Username, x.FullName, x.Email, x.Phone, x.IsActive, x.IsInternalAuthEnabled, x.MustChangePassword, x.TwoFactorEnabled, x.TwoFactorChannel))
            .ToListAsync(cancellationToken);
        await searchIndexService.BulkIndexUsersAsync(users, cancellationToken);
    }

    public async Task ReindexRolesAsync(CancellationToken cancellationToken)
    {
        await ClearIndexAsync(indexNames.Roles, cancellationToken);
        var roles = await dbContext.Roles.AsNoTracking()
            .Select(x => new RoleDto(x.Id, x.Name, x.Code, x.Description))
            .ToListAsync(cancellationToken);
        await searchIndexService.BulkIndexRolesAsync(roles, cancellationToken);
    }

    public async Task ReindexPermissionsAsync(CancellationToken cancellationToken)
    {
        await ClearIndexAsync(indexNames.Permissions, cancellationToken);
        var permissions = await dbContext.Permissions.AsNoTracking()
            .Select(x => new PermissionDto(x.Id, x.Domain, x.Bit, x.Code, x.Description, x.IsSystem))
            .ToListAsync(cancellationToken);
        await searchIndexService.BulkIndexPermissionsAsync(permissions, cancellationToken);
    }

    public async Task ReindexWorkspacesAsync(CancellationToken cancellationToken)
    {
        await ClearIndexAsync(indexNames.Workspaces, cancellationToken);
        var workspaces = await dbContext.Workspaces.AsNoTracking()
            .Select(x => new WorkspaceDto(x.Id, x.Name, x.Code, x.Description, x.IsSystem))
            .ToListAsync(cancellationToken);
        await searchIndexService.BulkIndexWorkspacesAsync(workspaces, cancellationToken);
    }

    public async Task ReindexApplicationsAsync(CancellationToken cancellationToken)
    {
        await ClearIndexAsync(indexNames.Applications, cancellationToken);
        var applications = await dbContext.Applications.AsNoTracking()
            .Select(x => new ApplicationDto(x.Id, x.Name, x.Description, x.ClientId, x.IsActive,
                x.IsConfidential, x.LogoUrl, x.HomepageUrl, x.RedirectUris, x.PostLogoutRedirectUris, x.AllowedOrigins,
                x.Scopes, x.GrantTypes, x.Audiences, x.AccessTokenLifetimeMinutes, x.RefreshTokenLifetimeMinutes))
            .ToListAsync(cancellationToken);
        await searchIndexService.BulkIndexApplicationsAsync(applications, cancellationToken);
    }

    public async Task ReindexServiceAccountsAsync(CancellationToken cancellationToken)
    {
        await ClearIndexAsync(indexNames.ServiceAccounts, cancellationToken);
        var serviceAccounts = await dbContext.ServiceAccounts.AsNoTracking()
            .Select(x => new ServiceAccountDto(x.Id, x.Name, x.Description, x.ClientId, x.IsActive, x.Audiences, x.AccessTokenLifetimeMinutes))
            .ToListAsync(cancellationToken);
        await searchIndexService.BulkIndexServiceAccountsAsync(serviceAccounts, cancellationToken);
    }

    private async Task ClearIndexAsync(string indexName, CancellationToken cancellationToken)
    {
        var response = await client.DeleteByQueryAsync<object>(d => d
            .Index(indexName)
            .Query(q => q.MatchAll())
            .Refresh(), cancellationToken);

        if (!response.IsValid)
        {
            throw new InvalidOperationException(response.DebugInformation);
        }
    }

    private async Task DeleteAllIndicesAsync(CancellationToken cancellationToken)
    {
        var all = string.Join(",", indexNames.Users, indexNames.Roles, indexNames.Permissions, indexNames.Workspaces, indexNames.Applications, indexNames.ServiceAccounts);
        await client.Indices.DeleteAsync(all, ct: cancellationToken);
    }

    // Returns true if the index needs reindexing (was created or recreated due to mapping conflict)
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

            if (putMappingResponse.IsValid)
            {
                return false;
            }

            // Mapping conflict (e.g. text→keyword) — recreate the index
            var deleteResponse = await client.Indices.DeleteAsync(indexName, ct: cancellationToken);
            if (!deleteResponse.IsValid)
            {
                throw new InvalidOperationException(deleteResponse.DebugInformation);
            }
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
