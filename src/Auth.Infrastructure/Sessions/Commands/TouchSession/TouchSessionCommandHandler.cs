using Auth.Application;
using Auth.Application.Sessions.Commands.TouchSession;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Sessions.Commands.TouchSession;

internal sealed class TouchSessionCommandHandler(
    AuthDbContext dbContext,
    IOptions<IntegrationOptions> options) : IRequestHandler<TouchSessionCommand>
{
    public async Task Handle(TouchSessionCommand command, CancellationToken ct)
    {
        var session = await dbContext.UserSessions
            .FirstOrDefaultAsync(s => s.Id == command.SessionId && s.UserId == command.UserId, ct);

        if (session is null || !session.IsActive)
            throw new AuthException(AuthErrorCatalog.SessionRevoked);

        session.TouchActivity(options.Value.Oidc.RefreshTokenLifetimeDays);
        await dbContext.SaveChangesAsync(ct);
    }
}
