using Auth.Application;
using Auth.Application.Permissions.Queries.GetAllPermissions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Permissions.Queries.GetAllPermissions;

internal sealed class GetAllPermissionsQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetAllPermissionsQuery, IReadOnlyCollection<PermissionDto>>
{
    public async Task<IReadOnlyCollection<PermissionDto>> Handle(GetAllPermissionsQuery query, CancellationToken cancellationToken) =>
        await dbContext.Permissions.AsNoTracking()
            .Select(x => new PermissionDto(x.Id, x.Domain, x.Bit, x.Code, x.Description, x.IsSystem))
            .ToListAsync(cancellationToken);
}
