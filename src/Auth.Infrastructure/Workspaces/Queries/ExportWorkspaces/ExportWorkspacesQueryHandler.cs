using Auth.Application;
using Auth.Application.Workspaces.Queries.ExportWorkspaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Workspaces.Queries.ExportWorkspaces;

internal sealed class ExportWorkspacesQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<ExportWorkspacesQuery, IReadOnlyCollection<ExportWorkspaceDto>>
{
    public async Task<IReadOnlyCollection<ExportWorkspaceDto>> Handle(ExportWorkspacesQuery query, CancellationToken cancellationToken) =>
        await dbContext.Workspaces
            .Where(w => !w.IsSystem)
            .OrderBy(w => w.Code)
            .Select(w => new ExportWorkspaceDto(w.Name, w.Code, w.Description))
            .ToListAsync(cancellationToken);
}
