using Auth.Application;
using Auth.Application.TwoFactor.Commands.DisableTwoFactor;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.TwoFactor.Commands.DisableTwoFactor;

internal sealed class DisableTwoFactorCommandHandler(
    AuthDbContext dbContext,
    ILogger<DisableTwoFactorCommandHandler> logger) : IRequestHandler<DisableTwoFactorCommand>
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
