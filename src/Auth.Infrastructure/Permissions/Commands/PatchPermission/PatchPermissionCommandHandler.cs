using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Application.Messaging.Events;
using Auth.Application.Permissions.Commands.PatchPermission;
using Auth.Infrastructure.AuditLogs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Permissions.Commands.PatchPermission;

internal sealed class PatchPermissionCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    IAuditContext auditContext) : IRequestHandler<PatchPermissionCommand, PermissionDto?>
{
    public async Task<PermissionDto?> Handle(PatchPermissionCommand command, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Permissions.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (entity is null)
            return null;

        entity.GuardNotSystem();

        if (command.Code.HasValue)
            entity.Code = command.Code.Value!;
        if (command.Description.HasValue)
            entity.Description = command.Description.Value!;

        var changes = AuditDiff.CaptureChanges(dbContext.Entry(entity));
        if (changes.Count > 0)
            auditContext.Details = changes;

        await eventBus.PublishAsync(new PermissionUpdatedEvent { PermissionId = entity.Id, Code = entity.Code }, cancellationToken);
        await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.Permission, EntityId = entity.Id, Operation = IndexOperation.Index }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return new PermissionDto(entity.Id, entity.Domain, entity.Bit, entity.Code, entity.Description, entity.IsSystem);
    }
}
