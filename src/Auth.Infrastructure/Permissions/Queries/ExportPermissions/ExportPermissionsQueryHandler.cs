using Auth.Application;
using Auth.Application.Permissions.Queries.ExportPermissions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Permissions.Queries.ExportPermissions;

internal sealed class ExportPermissionsQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<ExportPermissionsQuery, IReadOnlyCollection<ExportPermissionDto>>
{
    public async Task<IReadOnlyCollection<ExportPermissionDto>> Handle(ExportPermissionsQuery query, CancellationToken cancellationToken) =>
        await dbContext.Permissions
            .Where(x => !x.IsSystem)
            .OrderBy(x => x.Domain).ThenBy(x => x.Bit)
            .Select(x => new ExportPermissionDto(x.Domain, x.Bit, x.Code, x.Description))
            .ToListAsync(cancellationToken);
}
