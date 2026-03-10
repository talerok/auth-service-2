using Auth.Application;
using Auth.Application.Permissions.Queries.GetPermissionById;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Permissions.Queries.GetPermissionById;

internal sealed class GetPermissionByIdQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetPermissionByIdQuery, PermissionDto?>
{
    public async Task<PermissionDto?> Handle(GetPermissionByIdQuery query, CancellationToken cancellationToken) =>
        await dbContext.Permissions.AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new PermissionDto(x.Id, x.Domain, x.Bit, x.Code, x.Description, x.IsSystem))
            .FirstOrDefaultAsync(cancellationToken);
}
