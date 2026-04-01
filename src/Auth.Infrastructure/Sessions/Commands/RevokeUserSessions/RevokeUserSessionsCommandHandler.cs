using Auth.Application;
using Auth.Application.Messaging.Events;
using Auth.Application.Sessions.Commands.RevokeUserSessions;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Sessions.Commands.RevokeUserSessions;

internal sealed class RevokeUserSessionsCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    IAuditService auditService) : IRequestHandler<RevokeUserSessionsCommand>
{
    public async Task Handle(RevokeUserSessionsCommand command, CancellationToken cancellationToken)
    {
        if (!await dbContext.Users.AnyAsync(u => u.Id == command.UserId, cancellationToken))
            throw new AuthException(AuthErrorCatalog.UserNotFound);

        var sessions = await dbContext.UserSessions
            .Where(s => s.UserId == command.UserId && !s.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.Revoke(command.Reason);
            await eventBus.PublishAsync(new SessionRevokedEvent
            {
                SessionId = session.Id, UserId = command.UserId, Reason = command.Reason
            }, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            AuditEntityType.User, command.UserId, AuditAction.RevokeAllSessions,
            cancellationToken: cancellationToken);
    }
}
