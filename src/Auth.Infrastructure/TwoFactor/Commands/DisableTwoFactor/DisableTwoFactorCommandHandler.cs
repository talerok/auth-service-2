using Auth.Application;
using Auth.Application.TwoFactor.Commands.DisableTwoFactor;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.TwoFactor.Commands.DisableTwoFactor;

#pragma warning disable CS9113 // Parameter 'auditContext' is unread
internal sealed class DisableTwoFactorCommandHandler(
    AuthDbContext dbContext,
    IAuditContext auditContext,
    ILogger<DisableTwoFactorCommandHandler> logger) : IRequestHandler<DisableTwoFactorCommand>
#pragma warning restore CS9113
{
    public async Task Handle(DisableTwoFactorCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.UserId, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.UserNotFound);
        user.DisableTwoFactor();
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "TwoFactorOperation userId={UserId} operation={Operation} result={Result}",
            user.Id,
            "DISABLED",
            "SUCCESS");
    }
}
