using Auth.Application;
using Auth.Application.Workspaces.Queries.GetAllWorkspaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Workspaces.Queries.GetAllWorkspaces;

internal sealed class GetAllWorkspacesQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetAllWorkspacesQuery, IReadOnlyCollection<WorkspaceDto>>
{
    public async Task<IReadOnlyCollection<WorkspaceDto>> Handle(GetAllWorkspacesQuery query, CancellationToken cancellationToken) =>
        await dbContext.Workspaces.AsNoTracking()
            .Select(x => new WorkspaceDto(x.Id, x.Name, x.Code, x.Description, x.IsSystem))
            .ToListAsync(cancellationToken);
}
