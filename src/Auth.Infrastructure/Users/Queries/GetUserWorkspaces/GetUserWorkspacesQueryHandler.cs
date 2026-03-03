using Auth.Application;
using Auth.Application.Users.Queries.GetUserWorkspaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Users.Queries.GetUserWorkspaces;

internal sealed class GetUserWorkspacesQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetUserWorkspacesQuery, IReadOnlyCollection<UserWorkspaceRolesItem>?>
{
    public async Task<IReadOnlyCollection<UserWorkspaceRolesItem>?> Handle(
        GetUserWorkspacesQuery query, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Users.AnyAsync(x => x.Id == query.UserId, cancellationToken);
        if (!exists)
            return null;

        return await dbContext.UserWorkspaces
            .AsNoTracking()
            .Where(x => x.UserId == query.UserId)
            .Select(x => new UserWorkspaceRolesItem(
                x.WorkspaceId,
                x.UserWorkspaceRoles.Select(r => r.RoleId).ToList()))
            .ToListAsync(cancellationToken);
    }
}
