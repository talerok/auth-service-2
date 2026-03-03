using System.Security.Cryptography;
using Auth.Application;
using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.Infrastructure;

public sealed class ApiClientService(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IOpenIddictApplicationManager appManager) : IApiClientService
{
    public async Task<IReadOnlyCollection<ApiClientDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.ApiClients.AsNoTracking()
            .Select(x => new ApiClientDto(x.Id, x.Name, x.Description, x.ClientId, x.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<ApiClientDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.ApiClients.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ApiClientDto(x.Id, x.Name, x.Description, x.ClientId, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CreateApiClientResponse> CreateAsync(CreateApiClientRequest request, CancellationToken cancellationToken)
    {
        var clientId = $"ac-{Guid.NewGuid():N}";
        var clientSecret = GenerateSecret();

        var apiClient = new ApiClient
        {
            Name = request.Name,
            Description = request.Description,
            ClientId = clientId,
            IsActive = request.IsActive
        };

        dbContext.ApiClients.Add(apiClient);
        await dbContext.SaveChangesAsync(cancellationToken);

        await appManager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            DisplayName = request.Name,
            ClientType = ClientTypes.Confidential,
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.ClientCredentials,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Prefixes.Scope + "ws"
            }
        }, cancellationToken);

        var dto = new ApiClientDto(apiClient.Id, apiClient.Name, apiClient.Description, apiClient.ClientId, apiClient.IsActive);
        await searchIndexService.IndexApiClientAsync(dto, cancellationToken);
        return new CreateApiClientResponse(dto, clientSecret);
    }

    public async Task<ApiClientDto?> UpdateAsync(Guid id, UpdateApiClientRequest request, CancellationToken cancellationToken)
    {
        var apiClient = await dbContext.ApiClients.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (apiClient is null)
            return null;

        apiClient.Name = request.Name;
        apiClient.Description = request.Description;
        apiClient.IsActive = request.IsActive;
        apiClient.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var oidcApp = await appManager.FindByClientIdAsync(apiClient.ClientId, cancellationToken);
        if (oidcApp is not null)
        {
            var descriptor = new OpenIddictApplicationDescriptor();
            await appManager.PopulateAsync(descriptor, oidcApp, cancellationToken);
            descriptor.DisplayName = request.Name;
            await appManager.UpdateAsync(oidcApp, descriptor, cancellationToken);
        }

        var dto = new ApiClientDto(apiClient.Id, apiClient.Name, apiClient.Description, apiClient.ClientId, apiClient.IsActive);
        await searchIndexService.IndexApiClientAsync(dto, cancellationToken);
        return dto;
    }

    public async Task<ApiClientDto?> PatchAsync(Guid id, PatchApiClientRequest request, CancellationToken cancellationToken)
    {
        var apiClient = await dbContext.ApiClients.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (apiClient is null)
            return null;

        if (request.Name is not null)
            apiClient.Name = request.Name;

        if (request.Description is not null)
            apiClient.Description = request.Description;

        if (request.IsActive.HasValue)
            apiClient.IsActive = request.IsActive.Value;

        apiClient.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (request.Name is not null)
        {
            var oidcApp = await appManager.FindByClientIdAsync(apiClient.ClientId, cancellationToken);
            if (oidcApp is not null)
            {
                var descriptor = new OpenIddictApplicationDescriptor();
                await appManager.PopulateAsync(descriptor, oidcApp, cancellationToken);
                descriptor.DisplayName = apiClient.Name;
                await appManager.UpdateAsync(oidcApp, descriptor, cancellationToken);
            }
        }

        var dto = new ApiClientDto(apiClient.Id, apiClient.Name, apiClient.Description, apiClient.ClientId, apiClient.IsActive);
        await searchIndexService.IndexApiClientAsync(dto, cancellationToken);
        return dto;
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var apiClient = await dbContext.ApiClients.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (apiClient is null)
            return false;

        apiClient.DeletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var oidcApp = await appManager.FindByClientIdAsync(apiClient.ClientId, cancellationToken);
        if (oidcApp is not null)
            await appManager.DeleteAsync(oidcApp, cancellationToken);

        await searchIndexService.DeleteApiClientAsync(id, cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<ApiClientWorkspaceRolesItem>?> GetWorkspacesAsync(Guid apiClientId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.ApiClients.AnyAsync(x => x.Id == apiClientId, cancellationToken);
        if (!exists)
            return null;

        return await dbContext.ApiClientWorkspaces
            .AsNoTracking()
            .Where(x => x.ApiClientId == apiClientId)
            .Select(x => new ApiClientWorkspaceRolesItem(
                x.WorkspaceId,
                x.ApiClientWorkspaceRoles.Select(r => r.RoleId).ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task SetWorkspacesAsync(
        Guid apiClientId,
        IReadOnlyCollection<ApiClientWorkspaceRolesItem> workspaces,
        CancellationToken cancellationToken)
    {
        var workspacesById = workspaces
            .GroupBy(x => x.WorkspaceId)
            .ToDictionary(
                x => x.Key,
                x => x.SelectMany(y => y.RoleIds)
                    .Distinct()
                    .ToArray());

        var workspaceIds = workspacesById.Keys.ToArray();
        var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var current = await dbContext.ApiClientWorkspaces.Where(x => x.ApiClientId == apiClientId).ToListAsync(cancellationToken);
        var currentWorkspaceIds = current.Select(x => x.WorkspaceId).ToArray();
        var diff = CollectionDiff.Calculate(workspaceIds, currentWorkspaceIds);
        var toRemove = current.Where(x => diff.ToRemove.Contains(x.WorkspaceId)).ToArray();

        if (toRemove.Length > 0)
            dbContext.ApiClientWorkspaces.RemoveRange(toRemove);

        var targetByWorkspaceId = current
            .Where(x => !diff.ToRemove.Contains(x.WorkspaceId))
            .ToDictionary(x => x.WorkspaceId);

        foreach (var workspaceId in diff.ToAdd)
        {
            var acw = new ApiClientWorkspace { ApiClientId = apiClientId, WorkspaceId = workspaceId };
            dbContext.ApiClientWorkspaces.Add(acw);
            targetByWorkspaceId[workspaceId] = acw;
        }

        var targetIds = targetByWorkspaceId.Values.Select(x => x.Id).ToArray();
        var currentRoles = await dbContext.ApiClientWorkspaceRoles
            .Where(x => targetIds.Contains(x.ApiClientWorkspaceId))
            .ToListAsync(cancellationToken);

        foreach (var workspace in workspacesById)
        {
            var acw = targetByWorkspaceId[workspace.Key];
            var requestedRoleIds = workspace.Value;
            var wsCurrentRoles = currentRoles.Where(x => x.ApiClientWorkspaceId == acw.Id).ToArray();
            var currentRoleIds = wsCurrentRoles.Select(x => x.RoleId).ToArray();
            var roleDiff = CollectionDiff.Calculate(requestedRoleIds, currentRoleIds);
            var rolesToRemove = wsCurrentRoles.Where(x => roleDiff.ToRemove.Contains(x.RoleId)).ToArray();

            if (rolesToRemove.Length > 0)
                dbContext.ApiClientWorkspaceRoles.RemoveRange(rolesToRemove);

            foreach (var roleId in roleDiff.ToAdd)
                dbContext.ApiClientWorkspaceRoles.Add(new ApiClientWorkspaceRole { ApiClientWorkspaceId = acw.Id, RoleId = roleId });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    public async Task<RegenerateApiClientSecretResponse?> RegenerateSecretAsync(Guid id, CancellationToken cancellationToken)
    {
        var apiClient = await dbContext.ApiClients.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (apiClient is null)
            return null;

        var newSecret = GenerateSecret();

        var oidcApp = await appManager.FindByClientIdAsync(apiClient.ClientId, cancellationToken);
        if (oidcApp is not null)
        {
            var descriptor = new OpenIddictApplicationDescriptor();
            await appManager.PopulateAsync(descriptor, oidcApp, cancellationToken);
            descriptor.ClientSecret = newSecret;
            await appManager.UpdateAsync(oidcApp, descriptor, cancellationToken);
        }

        return new RegenerateApiClientSecretResponse(newSecret);
    }

    private static string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
