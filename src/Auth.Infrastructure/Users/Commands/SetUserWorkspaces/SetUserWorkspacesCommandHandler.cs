using Auth.Application;
using Auth.Application.Users.Commands.SetUserWorkspaces;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Users.Commands.SetUserWorkspaces;

internal sealed class SetUserWorkspacesCommandHandler(
    AuthDbContext dbContext) : IRequestHandler<SetUserWorkspacesCommand>
{
    public async Task Handle(SetUserWorkspacesCommand command, CancellationToken cancellationToken)
    {
        var workspacesById = command.Workspaces
            .GroupBy(x => x.WorkSpaceId)
            .ToDictionary(
                x => x.Key,
                x => x.SelectMany(y => y.RoleIds)
                    .Distinct()
                    .ToArray());

        var workspaceIds = workspacesById.Keys.ToArray();
        var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var current = await dbContext.UserWorkspaces.Where(x => x.UserId == command.UserId).ToListAsync(cancellationToken);
        var currentWorkspaceIds = current.Select(x => x.WorkspaceId).ToArray();
        var diff = CollectionDiff.Calculate(workspaceIds, currentWorkspaceIds);
        var toRemove = current.Where(x => diff.ToRemove.Contains(x.WorkspaceId)).ToArray();

        if (toRemove.Length > 0)
            dbContext.UserWorkspaces.RemoveRange(toRemove);

        var targetUserWorkspacesByWorkspaceId = current
            .Where(x => !diff.ToRemove.Contains(x.WorkspaceId))
            .ToDictionary(x => x.WorkspaceId);

        foreach (var workspaceId in diff.ToAdd)
        {
            var userWorkspace = new UserWorkspace { UserId = command.UserId, WorkspaceId = workspaceId };
            dbContext.UserWorkspaces.Add(userWorkspace);
            targetUserWorkspacesByWorkspaceId[workspaceId] = userWorkspace;
        }

        var targetUserWorkspaceIds = targetUserWorkspacesByWorkspaceId.Values.Select(x => x.Id).ToArray();
        var currentRoles = await dbContext.UserWorkspaceRoles
            .Where(x => targetUserWorkspaceIds.Contains(x.UserWorkspaceId))
            .ToListAsync(cancellationToken);

        foreach (var workspace in workspacesById)
        {
            var userWorkspace = targetUserWorkspacesByWorkspaceId[workspace.Key];
            var requestedRoleIds = workspace.Value;
            var workspaceCurrentRoles = currentRoles.Where(x => x.UserWorkspaceId == userWorkspace.Id).ToArray();
            var currentRoleIds = workspaceCurrentRoles.Select(x => x.RoleId).ToArray();
            var roleDiff = CollectionDiff.Calculate(requestedRoleIds, currentRoleIds);
            var rolesToRemove = workspaceCurrentRoles.Where(x => roleDiff.ToRemove.Contains(x.RoleId)).ToArray();

            if (rolesToRemove.Length > 0)
                dbContext.UserWorkspaceRoles.RemoveRange(rolesToRemove);

            foreach (var roleId in roleDiff.ToAdd)
                dbContext.UserWorkspaceRoles.Add(new UserWorkspaceRole { UserWorkspaceId = userWorkspace.Id, RoleId = roleId });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }
}
