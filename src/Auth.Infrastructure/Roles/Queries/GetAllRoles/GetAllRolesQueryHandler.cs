using Auth.Application;
using Auth.Application.Roles.Queries.GetAllRoles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Roles.Queries.GetAllRoles;

internal sealed class GetAllRolesQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetAllRolesQuery, IReadOnlyCollection<RoleDto>>
{
    public async Task<IReadOnlyCollection<RoleDto>> Handle(GetAllRolesQuery query, CancellationToken cancellationToken) =>
        await dbContext.Roles.AsNoTracking().Select(x => new RoleDto(x.Id, x.Name, x.Code, x.Description)).ToListAsync(cancellationToken);
}
