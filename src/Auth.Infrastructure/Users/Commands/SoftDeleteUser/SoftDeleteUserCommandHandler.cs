using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Application.Messaging.Events;
using Auth.Application.Users.Commands.SoftDeleteUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Users.Commands.SoftDeleteUser;

internal sealed class SoftDeleteUserCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    IAuditContext auditContext) : IRequestHandler<SoftDeleteUserCommand, bool>
{
    public async Task<bool> Handle(SoftDeleteUserCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (user is null)
            return false;

        auditContext.Details = new Dictionary<string, object?> { ["username"] = user.Username };
        user.SoftDelete();

        await eventBus.PublishAsync(new UserDeletedEvent { UserId = command.Id }, cancellationToken);
        await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.User, EntityId = command.Id, Operation = IndexOperation.Delete }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
