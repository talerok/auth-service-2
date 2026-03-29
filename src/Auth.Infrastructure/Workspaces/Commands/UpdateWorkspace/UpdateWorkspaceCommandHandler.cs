using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Application.Messaging.Events;
using Auth.Application.Workspaces.Commands.UpdateWorkspace;
using Auth.Infrastructure.AuditLogs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Workspaces.Commands.UpdateWorkspace;

internal sealed class UpdateWorkspaceCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    IAuditContext auditContext) : IRequestHandler<UpdateWorkspaceCommand, WorkspaceDto?>
{
    public async Task<WorkspaceDto?> Handle(UpdateWorkspaceCommand command, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Workspaces.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.GuardNotSystem();

        entity.Name = command.Name;
        entity.Code = command.Code;
        entity.Description = command.Description;

        var changes = AuditDiff.CaptureChanges(dbContext.Entry(entity));
        if (changes.Count > 0)
            auditContext.Details = changes;

        await eventBus.PublishAsync(new WorkspaceUpdatedEvent { WorkspaceId = entity.Id }, cancellationToken);
        await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.Workspace, EntityId = entity.Id, Operation = IndexOperation.Index }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new WorkspaceDto(entity.Id, entity.Name, entity.Code, entity.Description, entity.IsSystem);
        return dto;
    }
}
