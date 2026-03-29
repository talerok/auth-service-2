using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Application.Messaging.Events;
using Auth.Application.Permissions.Commands.UpdatePermission;
using Auth.Infrastructure.AuditLogs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Permissions.Commands.UpdatePermission;

internal sealed class UpdatePermissionCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    IAuditContext auditContext) : IRequestHandler<UpdatePermissionCommand, PermissionDto?>
{
    public async Task<PermissionDto?> Handle(UpdatePermissionCommand command, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Permissions.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (entity is null)
            return null;

        entity.GuardNotSystem();

        entity.Code = command.Code;
        entity.Description = command.Description;

        var changes = AuditDiff.CaptureChanges(dbContext.Entry(entity));
        if (changes.Count > 0)
            auditContext.Details = changes;

        await eventBus.PublishAsync(new PermissionUpdatedEvent { PermissionId = entity.Id, Code = entity.Code }, cancellationToken);
        await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.Permission, EntityId = entity.Id, Operation = IndexOperation.Index }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return new PermissionDto(entity.Id, entity.Domain, entity.Bit, entity.Code, entity.Description, entity.IsSystem);
    }
}
