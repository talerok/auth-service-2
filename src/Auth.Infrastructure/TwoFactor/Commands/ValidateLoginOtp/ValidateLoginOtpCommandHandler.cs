using Auth.Application;
using Auth.Application.TwoFactor.Commands.ValidateLoginOtp;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.TwoFactor.Commands.ValidateLoginOtp;

internal sealed class ValidateLoginOtpCommandHandler(
    AuthDbContext dbContext,
    ILogger<ValidateLoginOtpCommandHandler> logger) : IRequestHandler<ValidateLoginOtpCommand, User>
{
    public async Task<User> Handle(ValidateLoginOtpCommand command, CancellationToken cancellationToken)
    {
        TwoFactorValidation.ValidateChannelOrThrow(command.Channel);
        var challenge = await dbContext.TwoFactorChallenges
            .FirstOrDefaultAsync(
                x => x.Id == command.ChallengeId && x.Purpose == TwoFactorChallenge.PurposeLogin,
                cancellationToken);
        TwoFactorValidation.ValidateChallengeOrThrow(challenge, command.Channel);
        TwoFactorValidation.ValidateDeliveryStatusOrThrow(challenge!, TwoFactorChallenge.PurposeLogin);
        await TwoFactorValidation.VerifyOtpOrThrowAsync(challenge!, command.Otp, dbContext, logger, cancellationToken);

        challenge!.MarkVerified();
        var user = await dbContext.Users.FirstAsync(x => x.Id == challenge.UserId, cancellationToken);
        if (!user.TwoFactorEnabled)
        {
            throw new AuthException(TwoFactorErrorCatalog.NotRequired);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "TwoFactorOperation userId={UserId} operation={Operation} result={Result}",
            user.Id,
            "LOGIN_VERIFIED",
            "SUCCESS");

        return user;
    }
}
