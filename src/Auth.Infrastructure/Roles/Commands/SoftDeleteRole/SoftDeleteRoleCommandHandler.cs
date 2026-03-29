using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Application.Messaging.Events;
using Auth.Application.Roles.Commands.SoftDeleteRole;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Roles.Commands.SoftDeleteRole;

internal sealed class SoftDeleteRoleCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    IAuditContext auditContext) : IRequestHandler<SoftDeleteRoleCommand, bool>
{
    public async Task<bool> Handle(SoftDeleteRoleCommand command, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Roles.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        auditContext.Details = new Dictionary<string, object?> { ["name"] = entity.Name, ["code"] = entity.Code };
        entity.SoftDelete();

        await eventBus.PublishAsync(new RoleDeletedEvent { RoleId = command.Id }, cancellationToken);
        await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.Role, EntityId = command.Id, Operation = IndexOperation.Delete }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
