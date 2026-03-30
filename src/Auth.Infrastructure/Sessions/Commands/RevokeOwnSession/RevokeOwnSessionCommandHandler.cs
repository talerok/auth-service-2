using Auth.Application;
using Auth.Application.Sessions.Commands.RevokeOwnSession;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Sessions.Commands.RevokeOwnSession;

internal sealed class RevokeOwnSessionCommandHandler(
    AuthDbContext dbContext,
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
        await dbContext.SaveChangesAsync(ct);

        await auditService.LogAsync(
            AuditEntityType.Session, session.Id, AuditAction.RevokeSession,
            cancellationToken: ct);
    }
}
