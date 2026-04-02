using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Application.Messaging.Events;
using Auth.Application.Sessions.Commands.RevokeOwnSession;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Sessions.Commands.RevokeOwnSession;

internal sealed class RevokeOwnSessionCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    IAuditService auditService) : IRequestHandler<RevokeOwnSessionCommand>
{
    public async Task Handle(RevokeOwnSessionCommand command, CancellationToken cancellationToken)
    {
        var session = await dbContext.UserSessions
            .FirstOrDefaultAsync(s => s.Id == command.SessionId, cancellationToken);

        if (session is null || session.UserId != command.UserId)
            throw new AuthException(AuthErrorCatalog.SessionNotFound);

        if (session.IsRevoked)
            throw new AuthException(AuthErrorCatalog.SessionAlreadyRevoked);

        session.Revoke("logout");
        await eventBus.PublishAsync(new SessionRevokedEvent
        {
            SessionId = session.Id, UserId = session.UserId, Reason = "logout"
        }, cancellationToken);
        await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.Session, EntityId = session.Id, Operation = IndexOperation.Index }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            AuditEntityType.Session, session.Id, AuditAction.RevokeSession,
            cancellationToken: cancellationToken);
    }
}
