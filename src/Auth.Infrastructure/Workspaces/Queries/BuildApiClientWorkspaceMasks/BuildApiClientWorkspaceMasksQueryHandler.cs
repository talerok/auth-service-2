using Auth.Application;
using Auth.Application.Workspaces.Queries.BuildApiClientWorkspaceMasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Workspaces.Queries.BuildApiClientWorkspaceMasks;

internal sealed class BuildApiClientWorkspaceMasksQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<BuildApiClientWorkspaceMasksQuery, Dictionary<string, Dictionary<string, byte[]>>>
{
    public async Task<Dictionary<string, Dictionary<string, byte[]>>> Handle(BuildApiClientWorkspaceMasksQuery query, CancellationToken cancellationToken)
    {
        var matrix = await dbContext.ApiClientWorkspaces
            .Where(acw => acw.ApiClientId == query.ApiClientId)
            .Select(acw => new
            {
                WorkspaceCode = acw.Workspace!.Code,
                Permissions = acw.ApiClientWorkspaceRoles
                    .SelectMany(acwr => acwr.Role!.RolePermissions)
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
