using Auth.Application;
using Auth.Application.Workspaces.Queries.BuildWorkspaceMasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Workspaces.Queries.BuildWorkspaceMasks;

internal sealed class BuildWorkspaceMasksQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<BuildWorkspaceMasksQuery, Dictionary<string, Dictionary<string, byte[]>>>
{
    public async Task<Dictionary<string, Dictionary<string, byte[]>>> Handle(BuildWorkspaceMasksQuery query, CancellationToken cancellationToken)
    {
        var matrix = await dbContext.UserWorkspaces
            .Where(uw => uw.UserId == query.UserId)
            .Select(uw => new
            {
                WorkspaceCode = uw.Workspace!.Code,
                Permissions = uw.UserWorkspaceRoles
                    .SelectMany(uwr => uwr.Role!.RolePermissions)
                    .Select(rp => new { rp.Permission!.Domain, rp.Permission!.Bit })
            })
            .ToListAsync(cancellationToken);

        var result = new Dictionary<string, Dictionary<string, byte[]>>();
        foreach (var row in matrix)
        {
            var domainMasks = new Dictionary<string, byte[]>();
            foreach (var group in row.Permissions.GroupBy(p => p.Domain))
            {
                domainMasks[group.Key] = PermissionBitmask.BuildMask(group.Select(p => p.Bit));
            }

            result[row.WorkspaceCode] = domainMasks;
        }

        return result;
    }
}
