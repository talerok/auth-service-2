using Auth.Application;
using Auth.Application.Workspaces.Commands.SoftDeleteWorkspace;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Workspaces.Commands.SoftDeleteWorkspace;

internal sealed class SoftDeleteWorkspaceCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService) : IRequestHandler<SoftDeleteWorkspaceCommand, bool>
{
    public async Task<bool> Handle(SoftDeleteWorkspaceCommand command, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Workspaces.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (entity is null)
        {
            return false;
        }
        if (entity.IsSystem)
        {
            throw new AuthException(AuthErrorCatalog.SystemWorkspaceDeleteForbidden);
        }

        entity.DeletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await searchIndexService.DeleteWorkspaceAsync(command.Id, cancellationToken);
        return true;
    }
}
