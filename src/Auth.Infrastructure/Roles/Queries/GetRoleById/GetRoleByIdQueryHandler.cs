using Auth.Application;
using Auth.Application.Roles.Queries.GetRoleById;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Roles.Queries.GetRoleById;

internal sealed class GetRoleByIdQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetRoleByIdQuery, RoleDto?>
{
    public async Task<RoleDto?> Handle(GetRoleByIdQuery query, CancellationToken cancellationToken) =>
        await dbContext.Roles.AsNoTracking().Where(x => x.Id == query.Id).Select(x => new RoleDto(x.Id, x.Name, x.Code, x.Description)).FirstOrDefaultAsync(cancellationToken);
}
