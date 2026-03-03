using Auth.Application;
using Auth.Application.ApiClients.Queries.GetApiClientWorkspaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.ApiClients.Queries.GetApiClientWorkspaces;

internal sealed class GetApiClientWorkspacesQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetApiClientWorkspacesQuery, IReadOnlyCollection<ApiClientWorkspaceRolesItem>?>
{
    public async Task<IReadOnlyCollection<ApiClientWorkspaceRolesItem>?> Handle(GetApiClientWorkspacesQuery query, CancellationToken cancellationToken)
    {
        var exists = await dbContext.ApiClients.AnyAsync(x => x.Id == query.ApiClientId, cancellationToken);
        if (!exists)
            return null;

        return await dbContext.ApiClientWorkspaces
            .AsNoTracking()
            .Where(x => x.ApiClientId == query.ApiClientId)
            .Select(x => new ApiClientWorkspaceRolesItem(
                x.WorkspaceId,
                x.ApiClientWorkspaceRoles.Select(r => r.RoleId).ToList()))
            .ToListAsync(cancellationToken);
    }
}
