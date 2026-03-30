using Auth.Application;
using Auth.Application.Messaging.Events;
using Auth.Application.Sessions.Commands.RevokeSession;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Sessions.Commands.RevokeSession;

internal sealed class RevokeSessionCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    IAuditService auditService) : IRequestHandler<RevokeSessionCommand>
{
    public async Task Handle(RevokeSessionCommand command, CancellationToken ct)
    {
        var session = await dbContext.UserSessions
            .FirstOrDefaultAsync(s => s.Id == command.SessionId, ct);

        if (session is null)
            throw new AuthException(AuthErrorCatalog.SessionNotFound);

        if (session.IsRevoked)
            throw new AuthException(AuthErrorCatalog.SessionAlreadyRevoked);

        session.Revoke(command.Reason);
        await eventBus.PublishAsync(new SessionRevokedEvent
        {
            SessionId = session.Id, UserId = session.UserId, Reason = command.Reason
        }, ct);
        await dbContext.SaveChangesAsync(ct);

        await auditService.LogAsync(
            AuditEntityType.Session, session.Id, AuditAction.RevokeSession,
            cancellationToken: ct);
    }
}
