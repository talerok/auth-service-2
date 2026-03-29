using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Application.Messaging.Events;
using Auth.Application.Workspaces.Commands.SoftDeleteWorkspace;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace Auth.Infrastructure.Workspaces.Commands.SoftDeleteWorkspace;

internal sealed class SoftDeleteWorkspaceCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    IOpenIddictScopeManager scopeManager,
    IAuditContext auditContext) : IRequestHandler<SoftDeleteWorkspaceCommand, bool>
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

        auditContext.Details = new Dictionary<string, object?> { ["name"] = entity.Name, ["code"] = entity.Code };
        entity.SoftDelete();
        await eventBus.PublishAsync(new WorkspaceDeletedEvent { WorkspaceId = command.Id }, cancellationToken);
        await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.Workspace, EntityId = command.Id, Operation = IndexOperation.Delete }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var oidcScope = await scopeManager.FindByNameAsync($"ws:{entity.Code}", cancellationToken);
        if (oidcScope is not null)
            await scopeManager.DeleteAsync(oidcScope, cancellationToken);

        return true;
    }
}
