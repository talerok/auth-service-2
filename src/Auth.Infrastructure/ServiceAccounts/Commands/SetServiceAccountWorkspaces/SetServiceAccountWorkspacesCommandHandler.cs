using Auth.Application;
using Auth.Application.ServiceAccounts.Commands.SetServiceAccountWorkspaces;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OidcPermissions = OpenIddict.Abstractions.OpenIddictConstants.Permissions;

namespace Auth.Infrastructure.ServiceAccounts.Commands.SetServiceAccountWorkspaces;

internal sealed class SetServiceAccountWorkspacesCommandHandler(
    AuthDbContext dbContext,
    IOpenIddictApplicationManager appManager,
    IAuditContext auditContext) : IRequestHandler<SetServiceAccountWorkspacesCommand>
{
    public async Task Handle(SetServiceAccountWorkspacesCommand command, CancellationToken cancellationToken)
    {
        var workspacesById = command.Workspaces
            .GroupBy(x => x.WorkspaceId)
            .ToDictionary(
                x => x.Key,
                x => x.SelectMany(y => y.RoleIds)
                    .Distinct()
                    .ToArray());

        var workspaceIds = workspacesById.Keys.ToArray();
        var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var current = await dbContext.ServiceAccountWorkspaces
            .Include(x => x.ServiceAccountWorkspaceRoles)
            .Where(x => x.ServiceAccountId == command.ServiceAccountId)
            .ToListAsync(cancellationToken);

        var currentWorkspaceIds = current.Select(x => x.WorkspaceId).ToHashSet();
        var toRemove = current.Where(x => !workspaceIds.Contains(x.WorkspaceId)).ToList();
        if (toRemove.Count > 0)
            dbContext.ServiceAccountWorkspaces.RemoveRange(toRemove);

        var addedWorkspaceIds = workspaceIds.Where(id => !currentWorkspaceIds.Contains(id)).ToList();
        var removedWorkspaceIds = toRemove.Select(x => x.WorkspaceId).ToList();
        auditContext.Details = new Dictionary<string, object?>
        {
            ["added"] = addedWorkspaceIds,
            ["removed"] = removedWorkspaceIds
        };

        foreach (var workspaceId in workspaceIds)
        {
            var existing = current.FirstOrDefault(x => x.WorkspaceId == workspaceId);
            var desiredRoleIds = workspacesById[workspaceId];

            if (existing is null)
            {
                var saw = new ServiceAccountWorkspace
                {
                    ServiceAccountId = command.ServiceAccountId,
                    WorkspaceId = workspaceId
                };
                foreach (var roleId in desiredRoleIds)
                    saw.ServiceAccountWorkspaceRoles.Add(new ServiceAccountWorkspaceRole { RoleId = roleId });
                dbContext.ServiceAccountWorkspaces.Add(saw);
            }
            else
            {
                var existingRoleIds = existing.ServiceAccountWorkspaceRoles.Select(r => r.RoleId).ToHashSet();
                var rolesToRemove = existing.ServiceAccountWorkspaceRoles.Where(r => !desiredRoleIds.Contains(r.RoleId)).ToList();
                var rolesToAdd = desiredRoleIds.Where(id => !existingRoleIds.Contains(id)).ToArray();

                if (rolesToRemove.Count > 0)
                    dbContext.ServiceAccountWorkspaceRoles.RemoveRange(rolesToRemove);
                foreach (var roleId in rolesToAdd)
                    dbContext.ServiceAccountWorkspaceRoles.Add(new ServiceAccountWorkspaceRole { ServiceAccountWorkspaceId = existing.Id, RoleId = roleId });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        await SyncOidcScopePermissionsAsync(command.ServiceAccountId, workspaceIds, cancellationToken);
    }

    private async Task SyncOidcScopePermissionsAsync(
        Guid serviceAccountId, Guid[] workspaceIds, CancellationToken cancellationToken)
    {
        var sa = await dbContext.ServiceAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == serviceAccountId, cancellationToken);
        if (sa is null) return;

        var oidcApp = await appManager.FindByClientIdAsync(sa.ClientId, cancellationToken);
        if (oidcApp is null) return;

        var workspaceCodes = await dbContext.Workspaces
            .Where(w => workspaceIds.Contains(w.Id))
            .Select(w => w.Code)
            .ToListAsync(cancellationToken);

        var descriptor = new OpenIddictApplicationDescriptor();
        await appManager.PopulateAsync(descriptor, oidcApp, cancellationToken);

        // Remove old workspace scope permissions
        descriptor.Permissions.RemoveWhere(p =>
            p.StartsWith(OidcPermissions.Prefixes.Scope + "ws:", StringComparison.Ordinal));

        // Add ws:* (always) + ws:{code} for each assigned workspace
        descriptor.Permissions.Add(OidcPermissions.Prefixes.Scope + "ws:*");
        foreach (var code in workspaceCodes)
            descriptor.Permissions.Add(OidcPermissions.Prefixes.Scope + $"ws:{code}");

        await appManager.UpdateAsync(oidcApp, descriptor, cancellationToken);
    }
}
