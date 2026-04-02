using Auth.Application;
using Auth.Application.Messaging;
using Auth.Application.Messaging.Commands;
using Auth.Application.Sessions.Commands.TouchSession;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Sessions.Commands.TouchSession;

internal sealed class TouchSessionCommandHandler(
    AuthDbContext dbContext,
    IOptions<IntegrationOptions> options,
    IEventBus eventBus) : IRequestHandler<TouchSessionCommand>
{
    public async Task Handle(TouchSessionCommand command, CancellationToken cancellationToken)
    {
        var session = await dbContext.UserSessions
            .FirstOrDefaultAsync(s => s.Id == command.SessionId && s.UserId == command.UserId, cancellationToken);

        if (session is null || !session.IsActive)
            throw new AuthException(AuthErrorCatalog.SessionRevoked);

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);
        if (user is null)
        {
            session.Revoke("User not found");
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new AuthException(AuthErrorCatalog.SessionRevoked);
        }

        if (user.IsLockedOut)
        {
            session.Revoke("Account locked out");
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new AuthException(AuthErrorCatalog.AccountLockedOut);
        }

        session.TouchActivity(options.Value.Oidc.RefreshTokenLifetimeDays);
        await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.Session, EntityId = session.Id, Operation = IndexOperation.Index }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
