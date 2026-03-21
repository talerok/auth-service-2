using Auth.Application.ServiceAccounts.Commands.SetServiceAccountWorkspaces;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.ServiceAccounts.Commands.SetServiceAccountWorkspaces;

internal sealed class SetServiceAccountWorkspacesCommandHandler(
    AuthDbContext dbContext) : IRequestHandler<SetServiceAccountWorkspacesCommand>
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

        var toRemove = current.Where(x => !workspaceIds.Contains(x.WorkspaceId)).ToList();
        if (toRemove.Count > 0)
            dbContext.ServiceAccountWorkspaces.RemoveRange(toRemove);

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
    }
}
