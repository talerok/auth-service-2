using Auth.Application;
using Auth.Application.Sessions.Commands.RevokeUserSessions;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Sessions.Commands.RevokeUserSessions;

internal sealed class RevokeUserSessionsCommandHandler(
    AuthDbContext dbContext,
    IAuditService auditService) : IRequestHandler<RevokeUserSessionsCommand>
{
    public async Task Handle(RevokeUserSessionsCommand command, CancellationToken ct)
    {
        if (!await dbContext.Users.AnyAsync(u => u.Id == command.UserId, ct))
            throw new AuthException(AuthErrorCatalog.UserNotFound);

        var sessions = await dbContext.UserSessions
            .Where(s => s.UserId == command.UserId && !s.IsRevoked)
            .ToListAsync(ct);

        foreach (var session in sessions)
            session.Revoke(command.Reason);

        await dbContext.SaveChangesAsync(ct);

        await auditService.LogAsync(
            AuditEntityType.User, command.UserId, AuditAction.RevokeAllSessions,
            cancellationToken: ct);
    }
}
