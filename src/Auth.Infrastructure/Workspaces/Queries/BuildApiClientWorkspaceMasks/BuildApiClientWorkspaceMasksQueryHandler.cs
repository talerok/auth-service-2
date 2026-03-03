using Auth.Application;
using Auth.Application.Workspaces.Queries.BuildApiClientWorkspaceMasks;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Workspaces.Queries.BuildApiClientWorkspaceMasks;

internal sealed class BuildApiClientWorkspaceMasksQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<BuildApiClientWorkspaceMasksQuery, Dictionary<string, byte[]>>
{
    public async Task<Dictionary<string, byte[]>> Handle(BuildApiClientWorkspaceMasksQuery query, CancellationToken cancellationToken)
    {
        var matrix = await dbContext.ApiClientWorkspaces
            .Where(acw => acw.ApiClientId == query.ApiClientId)
            .Select(acw => new
            {
                acw.Workspace!.Code,
                Bits = acw.ApiClientWorkspaceRoles
                    .SelectMany(acwr => acwr.Role!.RolePermissions)
                    .Select(rp => rp.Permission!.Bit)
            })
            .ToListAsync(cancellationToken);

        var result = new Dictionary<string, byte[]>();
        foreach (var row in matrix)
        {
            result[row.Code] = PermissionBitmask.BuildMask(row.Bits);
        }

        return result;
    }
}
