using Auth.Application;
using Auth.Application.Workspaces.Queries.BuildServiceAccountWorkspaceMasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Workspaces.Queries.BuildServiceAccountWorkspaceMasks;

internal sealed class BuildServiceAccountWorkspaceMasksQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<BuildServiceAccountWorkspaceMasksQuery, Dictionary<string, Dictionary<string, byte[]>>>
{
    public async Task<Dictionary<string, Dictionary<string, byte[]>>> Handle(BuildServiceAccountWorkspaceMasksQuery query, CancellationToken cancellationToken)
    {
        var matrix = await dbContext.ServiceAccountWorkspaces
            .Where(saw => saw.ServiceAccountId == query.ServiceAccountId)
            .Select(saw => new
            {
                WorkspaceCode = saw.Workspace!.Code,
                Permissions = saw.ServiceAccountWorkspaceRoles
                    .SelectMany(sawr => sawr.Role!.RolePermissions)
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
