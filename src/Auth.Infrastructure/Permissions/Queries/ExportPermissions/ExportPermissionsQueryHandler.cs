using Auth.Application;
using Auth.Application.Permissions.Queries.ExportPermissions;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Permissions.Queries.ExportPermissions;

internal sealed class ExportPermissionsQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<ExportPermissionsQuery, IReadOnlyCollection<ExportPermissionDto>>
{
    public async Task<IReadOnlyCollection<ExportPermissionDto>> Handle(ExportPermissionsQuery query, CancellationToken cancellationToken) =>
        await dbContext.Permissions
            .Where(x => x.Bit >= SystemPermissionCatalog.CustomBitStart)
            .OrderBy(x => x.Bit)
            .Select(x => new ExportPermissionDto(x.Bit, x.Code, x.Description))
            .ToListAsync(cancellationToken);
}
