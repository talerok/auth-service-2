using Auth.Application;
using Auth.Application.ApiClients.Commands.SetApiClientWorkspaces;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.ApiClients.Commands.SetApiClientWorkspaces;

internal sealed class SetApiClientWorkspacesCommandHandler(
    AuthDbContext dbContext) : IRequestHandler<SetApiClientWorkspacesCommand>
{
    public async Task Handle(SetApiClientWorkspacesCommand command, CancellationToken cancellationToken)
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
        var current = await dbContext.ApiClientWorkspaces
            .Where(x => x.ApiClientId == command.ApiClientId)
            .ToListAsync(cancellationToken);
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
            var acw = new ApiClientWorkspace { ApiClientId = command.ApiClientId, WorkspaceId = workspaceId };
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
}
