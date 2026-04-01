using Auth.Application;
using Auth.Application.Sessions.Commands.CreateSession;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Sessions.Commands.CreateSession;

internal sealed class CreateSessionCommandHandler(
    AuthDbContext dbContext,
    IOptions<IntegrationOptions> options,
    IAuditService auditService) : IRequestHandler<CreateSessionCommand, Guid>
{
    public async Task<Guid> Handle(CreateSessionCommand command, CancellationToken cancellationToken)
    {
        Guid? applicationId = null;
        if (!string.IsNullOrWhiteSpace(command.ClientId))
        {
            applicationId = await dbContext.Applications
                .Where(a => a.ClientId == command.ClientId)
                .Select(a => (Guid?)a.Id)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new AuthException(AuthErrorCatalog.ApplicationNotFound);
        }

        var session = UserSession.Create(
            command.UserId,
            command.IpAddress,
            command.UserAgent,
            applicationId,
            command.AuthMethod,
            options.Value.Oidc.RefreshTokenLifetimeDays);

        dbContext.UserSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            AuditEntityType.Session, session.Id, AuditAction.CreateSession,
            details: new Dictionary<string, object?>
            {
                ["authMethod"] = command.AuthMethod,
                ["applicationId"] = applicationId,
                ["clientId"] = command.ClientId,
                ["ipAddress"] = command.IpAddress
            },
            cancellationToken: cancellationToken);

        return session.Id;
    }
}
