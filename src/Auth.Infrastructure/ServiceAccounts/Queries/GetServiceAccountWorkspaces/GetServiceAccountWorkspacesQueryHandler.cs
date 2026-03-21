using Auth.Application;
using Auth.Application.ServiceAccounts.Queries.GetServiceAccountWorkspaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.ServiceAccounts.Queries.GetServiceAccountWorkspaces;

internal sealed class GetServiceAccountWorkspacesQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetServiceAccountWorkspacesQuery, IReadOnlyCollection<ServiceAccountWorkspaceRolesItem>?>
{
    public async Task<IReadOnlyCollection<ServiceAccountWorkspaceRolesItem>?> Handle(
        GetServiceAccountWorkspacesQuery query, CancellationToken cancellationToken)
    {
        var exists = await dbContext.ServiceAccounts.AnyAsync(x => x.Id == query.ServiceAccountId, cancellationToken);
        if (!exists)
            return null;

        return await dbContext.ServiceAccountWorkspaces
            .AsNoTracking()
            .Where(x => x.ServiceAccountId == query.ServiceAccountId)
            .Select(x => new ServiceAccountWorkspaceRolesItem(
                x.WorkspaceId,
                x.ServiceAccountWorkspaceRoles.Select(r => r.RoleId).ToList()))
            .ToListAsync(cancellationToken);
    }
}
