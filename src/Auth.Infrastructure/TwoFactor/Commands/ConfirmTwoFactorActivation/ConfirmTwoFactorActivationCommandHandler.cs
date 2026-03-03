using Auth.Application;
using Auth.Application.TwoFactor.Commands.ConfirmTwoFactorActivation;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.TwoFactor.Commands.ConfirmTwoFactorActivation;

internal sealed class ConfirmTwoFactorActivationCommandHandler(
    AuthDbContext dbContext,
    ILogger<ConfirmTwoFactorActivationCommandHandler> logger) : IRequestHandler<ConfirmTwoFactorActivationCommand>
{
    public async Task Handle(ConfirmTwoFactorActivationCommand command, CancellationToken cancellationToken)
    {
        TwoFactorValidation.ValidateChannelOrThrow(command.Channel);
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.UserId, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.UserNotFound);
        var challenge = await dbContext.TwoFactorChallenges
            .FirstOrDefaultAsync(
                x => x.Id == command.ChallengeId && x.UserId == user.Id && x.Purpose == TwoFactorChallenge.PurposeActivation,
                cancellationToken);

        TwoFactorValidation.ValidateChallengeOrThrow(challenge, command.Channel);
        TwoFactorValidation.ValidateDeliveryStatusOrThrow(challenge!, TwoFactorChallenge.PurposeActivation);
        await TwoFactorValidation.VerifyOtpOrThrowAsync(challenge!, command.Otp, dbContext, logger, cancellationToken);

        user.EnableTwoFactor(command.Channel);
        challenge!.MarkVerified();
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "TwoFactorOperation userId={UserId} operation={Operation} result={Result}",
            user.Id,
            "ACTIVATION_CONFIRMED",
            "SUCCESS");
    }
}
