using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Application.Messaging.Events;
using Auth.Application.Workspaces.Commands.CreateWorkspace;
using Auth.Domain;
using Auth.Infrastructure.AuditLogs;
using MediatR;
using OpenIddict.Abstractions;

namespace Auth.Infrastructure.Workspaces.Commands.CreateWorkspace;

internal sealed class CreateWorkspaceCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    IOpenIddictScopeManager scopeManager,
    IAuditContext auditContext) : IRequestHandler<CreateWorkspaceCommand, WorkspaceDto>
{
    public async Task<WorkspaceDto> Handle(CreateWorkspaceCommand command, CancellationToken cancellationToken)
    {
        var entity = new Workspace { Id = command.EntityId, Name = command.Name, Code = command.Code, Description = command.Description, IsSystem = command.IsSystem };
        dbContext.Workspaces.Add(entity);
        auditContext.Details = AuditDiff.CaptureState(dbContext.Entry(entity));
        await eventBus.PublishAsync(new WorkspaceCreatedEvent { WorkspaceId = entity.Id, Code = entity.Code }, cancellationToken);
        await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.Workspace, EntityId = entity.Id, Operation = IndexOperation.Index }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
        {
            Name = $"ws:{entity.Code}",
            DisplayName = $"Workspace: {entity.Code}"
        }, cancellationToken);

        return new WorkspaceDto(entity.Id, entity.Name, entity.Code, entity.Description, entity.IsSystem);
    }
}
