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
        await EnsureIndexExistsAsync(indexNames.Users, cancellationToken);
        await EnsureIndexExistsAsync(indexNames.Roles, cancellationToken);
        await EnsureIndexExistsAsync(indexNames.Permissions, cancellationToken);
        await EnsureIndexExistsAsync(indexNames.Workspaces, cancellationToken);
    }

    public async Task ReindexAllAsync(CancellationToken cancellationToken)
    {
        var users = await dbContext.Users.AsNoTracking()
            .Select(x => new UserDto(x.Id, x.Username, x.Email, x.IsActive, x.MustChangePassword))
            .ToListAsync(cancellationToken);
        foreach (var user in users)
        {
            await searchIndexService.IndexUserAsync(user, cancellationToken);
        }

        var roles = await dbContext.Roles.AsNoTracking()
            .Select(x => new RoleDto(x.Id, x.Name, x.Description))
            .ToListAsync(cancellationToken);
        foreach (var role in roles)
        {
            await searchIndexService.IndexRoleAsync(role, cancellationToken);
        }

        var permissions = await dbContext.Permissions.AsNoTracking()
            .Select(x => new PermissionDto(x.Id, x.Bit, x.Code, x.Description, x.IsSystem))
            .ToListAsync(cancellationToken);
        foreach (var permission in permissions)
        {
            await searchIndexService.IndexPermissionAsync(permission, cancellationToken);
        }

        var workspaces = await dbContext.Workspaces.AsNoTracking()
            .Select(x => new WorkspaceDto(x.Id, x.Name, x.Description, x.IsSystem))
            .ToListAsync(cancellationToken);
        foreach (var workspace in workspaces)
        {
            await searchIndexService.IndexWorkspaceAsync(workspace, cancellationToken);
        }
    }

    private async Task EnsureIndexExistsAsync(string indexName, CancellationToken cancellationToken)
    {
        var existsResponse = await client.Indices.ExistsAsync(indexName, ct: cancellationToken);
        if (existsResponse.Exists)
        {
            return;
        }

        var createResponse = await client.Indices.CreateAsync(indexName, _ => _, cancellationToken);
        if (!createResponse.IsValid)
        {
            throw new InvalidOperationException(createResponse.DebugInformation);
        }
    }
}
