using Auth.Application;
using Auth.Application.Roles.Queries.ExportRoles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Roles.Queries.ExportRoles;

internal sealed class ExportRolesQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<ExportRolesQuery, IReadOnlyCollection<ExportRoleDto>>
{
    public async Task<IReadOnlyCollection<ExportRoleDto>> Handle(ExportRolesQuery query, CancellationToken cancellationToken)
    {
        var roles = await dbContext.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return roles.Select(r => new ExportRoleDto(
            r.Name,
            r.Code,
            r.Description,
            r.RolePermissions
                .Where(rp => rp.Permission is not null)
                .Select(rp => rp.Permission!.Code)
                .OrderBy(c => c)
                .ToList()
        )).ToList();
    }
}
