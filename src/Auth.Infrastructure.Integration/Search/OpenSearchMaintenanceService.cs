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

        if (await EnsureIndexExistsAsync<AuditLogDto>(indexNames.AuditLogs, p => p
                .Keyword(k => k.Name(n => n.Id))
                .Keyword(k => k.Name(n => n.ActorId))
                .Keyword(k => k.Name(n => n.ActorName))
                .Keyword(k => k.Name(n => n.ActorType))
                .Keyword(k => k.Name(n => n.EntityType))
                .Keyword(k => k.Name(n => n.EntityId))
                .Keyword(k => k.Name(n => n.Action))
                .Keyword(k => k.Name(n => n.CorrelationId))
                .Keyword(k => k.Name(n => n.IpAddress))
                .Keyword(k => k.Name(n => n.UserAgent))
                .Date(d => d.Name(n => n.Timestamp)), cancellationToken))
        {
            await ReindexAuditLogsAsync(cancellationToken);
        }

        if (await EnsureIndexExistsAsync<NotificationTemplateDto>(indexNames.NotificationTemplates, p => p
                .Keyword(k => k.Name(n => n.Id))
                .Keyword(k => k.Name(n => n.Type))
                .Keyword(k => k.Name(n => n.Locale))
                .Keyword(k => k.Name(n => n.Subject))
                .Keyword(k => k.Name(n => n.Body)), cancellationToken))
        {
            await ReindexNotificationTemplatesAsync(cancellationToken);
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
            .Select(x => new UserDto(x.Id, x.Username, x.FullName, x.Email, x.Phone, x.IsActive, x.IsInternalAuthEnabled, x.MustChangePassword, x.TwoFactorEnabled, x.TwoFactorChannel, x.Locale, x.EmailVerified, x.PhoneVerified))
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
                x.Scopes, x.GrantTypes, x.Audiences, x.AccessTokenLifetimeMinutes, x.RefreshTokenLifetimeMinutes,
                x.RequireEmailVerified, x.RequirePhoneVerified))
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

    public async Task ReindexAuditLogsAsync(CancellationToken cancellationToken)
    {
        await ClearIndexAsync(indexNames.AuditLogs, cancellationToken);
        var rawEntries = await dbContext.AuditLogEntries.AsNoTracking().ToListAsync(cancellationToken);
        var entries = rawEntries.Select(x => new AuditLogDto(
                x.Id, x.Timestamp, x.ActorId, x.ActorName,
                AuditLogDto.CamelCase(x.ActorType),
                AuditLogDto.CamelCase(x.EntityType),
                x.EntityId,
                AuditLogDto.CamelCase(x.Action),
                x.Details, x.IpAddress, x.UserAgent, x.CorrelationId))
            .ToList();
        await searchIndexService.BulkIndexAuditLogsAsync(entries, cancellationToken);
    }

    public async Task ReindexNotificationTemplatesAsync(CancellationToken cancellationToken)
    {
        await ClearIndexAsync(indexNames.NotificationTemplates, cancellationToken);
        var templates = await dbContext.NotificationTemplates.AsNoTracking()
            .Select(x => new NotificationTemplateDto(x.Id, x.Type.ToString(), x.Locale, x.Subject, x.Body))
            .ToListAsync(cancellationToken);
        await searchIndexService.BulkIndexNotificationTemplatesAsync(templates, cancellationToken);
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
        var all = string.Join(",", indexNames.Users, indexNames.Roles, indexNames.Permissions, indexNames.Workspaces, indexNames.Applications, indexNames.ServiceAccounts, indexNames.AuditLogs, indexNames.NotificationTemplates);
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
