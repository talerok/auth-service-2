using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Application.Messaging.Events;
using Auth.Application.Roles.Commands.UpdateRole;
using Auth.Infrastructure.AuditLogs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Roles.Commands.UpdateRole;

internal sealed class UpdateRoleCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    IAuditContext auditContext) : IRequestHandler<UpdateRoleCommand, RoleDto?>
{
    public async Task<RoleDto?> Handle(UpdateRoleCommand command, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Roles.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Name = command.Name;
        entity.Code = command.Code;
        entity.Description = command.Description;
        var changes = AuditDiff.CaptureChanges(dbContext.Entry(entity));
        if (changes.Count > 0)
            auditContext.Details = changes;
        await eventBus.PublishAsync(new RoleUpdatedEvent { RoleId = entity.Id, ChangedFields = changes.Keys.ToArray() }, cancellationToken);
        await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.Role, EntityId = entity.Id, Operation = IndexOperation.Index }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new RoleDto(entity.Id, entity.Name, entity.Code, entity.Description);
        return dto;
    }
}
