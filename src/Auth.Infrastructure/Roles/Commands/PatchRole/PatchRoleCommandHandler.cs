using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Application.Messaging.Events;
using Auth.Application.Roles.Commands.PatchRole;
using Auth.Infrastructure.AuditLogs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Roles.Commands.PatchRole;

internal sealed class PatchRoleCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    IAuditContext auditContext) : IRequestHandler<PatchRoleCommand, RoleDto?>
{
    public async Task<RoleDto?> Handle(PatchRoleCommand command, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Roles.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (command.Name.HasValue)
            entity.Name = command.Name.Value!;

        if (command.Code.HasValue)
            entity.Code = command.Code.Value!;

        if (command.Description.HasValue)
            entity.Description = command.Description.Value!;

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
