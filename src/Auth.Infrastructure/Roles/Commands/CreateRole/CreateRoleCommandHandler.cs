using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Application.Messaging.Events;
using Auth.Application.Roles.Commands.CreateRole;
using Auth.Domain;
using Auth.Infrastructure.AuditLogs;
using MediatR;

namespace Auth.Infrastructure.Roles.Commands.CreateRole;

internal sealed class CreateRoleCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    IAuditContext auditContext) : IRequestHandler<CreateRoleCommand, RoleDto>
{
    public async Task<RoleDto> Handle(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        var entity = new Role { Id = command.EntityId, Name = command.Name, Code = command.Code, Description = command.Description };
        dbContext.Roles.Add(entity);
        auditContext.Details = AuditDiff.CaptureState(dbContext.Entry(entity));
        await eventBus.PublishAsync(new RoleCreatedEvent { RoleId = entity.Id, Name = entity.Name }, cancellationToken);
        await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.Role, EntityId = entity.Id, Operation = IndexOperation.Index }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new RoleDto(entity.Id, entity.Name, entity.Code, entity.Description);
        return dto;
    }
}
