using Auth.Application;
using Auth.Application.Roles.Queries.GetRolePermissions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Roles.Queries.GetRolePermissions;

internal sealed class GetRolePermissionsQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetRolePermissionsQuery, IReadOnlyCollection<PermissionDto>?>
{
    public async Task<IReadOnlyCollection<PermissionDto>?> Handle(GetRolePermissionsQuery query, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Roles.AnyAsync(x => x.Id == query.RoleId, cancellationToken);
        if (!exists)
            return null;

        return await dbContext.RolePermissions
            .AsNoTracking()
            .Where(x => x.RoleId == query.RoleId)
            .Select(x => new PermissionDto(x.Permission!.Id, x.Permission.Domain, x.Permission.Bit, x.Permission.Code, x.Permission.Description, x.Permission.IsSystem))
            .ToListAsync(cancellationToken);
    }
}
