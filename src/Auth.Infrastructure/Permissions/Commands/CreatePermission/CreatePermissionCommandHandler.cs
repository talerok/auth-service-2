using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Application.Messaging.Events;
using Auth.Application.Permissions.Commands.CreatePermission;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Permissions.Commands.CreatePermission;

internal sealed class CreatePermissionCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus) : IRequestHandler<CreatePermissionCommand, PermissionDto>
{
    public async Task<PermissionDto> Handle(CreatePermissionCommand command, CancellationToken cancellationToken)
    {
        var maxBit = await dbContext.Permissions.IgnoreQueryFilters()
            .Where(x => x.Domain == command.Domain)
            .Select(x => (int?)x.Bit)
            .MaxAsync(cancellationToken) ?? -1;

        var entity = new Permission
        {
            Domain = command.Domain,
            Bit = maxBit + 1,
            Code = command.Code,
            Description = command.Description,
            IsSystem = false
        };
        dbContext.Permissions.Add(entity);

        await eventBus.PublishAsync(new PermissionCreatedEvent { PermissionId = entity.Id, Code = entity.Code, Domain = entity.Domain, Bit = entity.Bit }, cancellationToken);
        await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.Permission, EntityId = entity.Id, Operation = IndexOperation.Index }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return new PermissionDto(entity.Id, entity.Domain, entity.Bit, entity.Code, entity.Description, entity.IsSystem);
    }
}
