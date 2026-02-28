using Auth.Application;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure;

public sealed class WorkspaceMaskService(AuthDbContext dbContext) : IWorkspaceMaskService
{
    public async Task<Dictionary<string, byte[]>> BuildWorkspaceMasksAsync(Guid userId, CancellationToken cancellationToken)
    {
        var matrix = await dbContext.UserWorkspaces
            .Where(uw => uw.UserId == userId)
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
