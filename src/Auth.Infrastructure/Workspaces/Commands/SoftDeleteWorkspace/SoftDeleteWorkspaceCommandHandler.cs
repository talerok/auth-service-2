using Auth.Application;
using Auth.Application.Workspaces.Commands.SoftDeleteWorkspace;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace Auth.Infrastructure.Workspaces.Commands.SoftDeleteWorkspace;

internal sealed class SoftDeleteWorkspaceCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IOpenIddictScopeManager scopeManager) : IRequestHandler<SoftDeleteWorkspaceCommand, bool>
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

        entity.SoftDelete();
        await dbContext.SaveChangesAsync(cancellationToken);

        var oidcScope = await scopeManager.FindByNameAsync($"ws:{entity.Code}", cancellationToken);
        if (oidcScope is not null)
            await scopeManager.DeleteAsync(oidcScope, cancellationToken);

        await searchIndexService.DeleteWorkspaceAsync(command.Id, cancellationToken);
        return true;
    }
}
