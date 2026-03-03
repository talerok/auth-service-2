using Auth.Application;
using Auth.Application.Workspaces.Queries.GetWorkspaceById;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Workspaces.Queries.GetWorkspaceById;

internal sealed class GetWorkspaceByIdQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetWorkspaceByIdQuery, WorkspaceDto?>
{
    public async Task<WorkspaceDto?> Handle(GetWorkspaceByIdQuery query, CancellationToken cancellationToken) =>
        await dbContext.Workspaces.AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new WorkspaceDto(x.Id, x.Name, x.Code, x.Description, x.IsSystem))
            .FirstOrDefaultAsync(cancellationToken);
}
