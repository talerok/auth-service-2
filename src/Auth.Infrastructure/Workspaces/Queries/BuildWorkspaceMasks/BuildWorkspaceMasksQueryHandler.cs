using Auth.Application;
using Auth.Application.Workspaces.Queries.BuildWorkspaceMasks;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Workspaces.Queries.BuildWorkspaceMasks;

internal sealed class BuildWorkspaceMasksQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<BuildWorkspaceMasksQuery, Dictionary<string, byte[]>>
{
    public async Task<Dictionary<string, byte[]>> Handle(BuildWorkspaceMasksQuery query, CancellationToken cancellationToken)
    {
        var matrix = await dbContext.UserWorkspaces
            .Where(uw => uw.UserId == query.UserId)
            .Select(uw => new
            {
                uw.Workspace!.Code,
                Bits = uw.UserWorkspaceRoles
                    .SelectMany(uwr => uwr.Role!.RolePermissions)
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
