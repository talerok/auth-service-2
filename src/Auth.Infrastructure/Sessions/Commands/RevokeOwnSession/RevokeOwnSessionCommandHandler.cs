using Auth.Application;
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
    public async Task Handle(RevokeOwnSessionCommand command, CancellationToken ct)
    {
        var session = await dbContext.UserSessions
            .FirstOrDefaultAsync(s => s.Id == command.SessionId, ct);

        if (session is null || session.UserId != command.UserId)
            throw new AuthException(AuthErrorCatalog.SessionNotFound);

        if (session.IsRevoked)
            throw new AuthException(AuthErrorCatalog.SessionAlreadyRevoked);

        session.Revoke("logout");
        await eventBus.PublishAsync(new SessionRevokedEvent
        {
            SessionId = session.Id, UserId = session.UserId, Reason = "logout"
        }, ct);
        await dbContext.SaveChangesAsync(ct);

        await auditService.LogAsync(
            AuditEntityType.Session, session.Id, AuditAction.RevokeSession,
            cancellationToken: ct);
    }
}
