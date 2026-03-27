using Auth.Application;
using Auth.Application.Verification;
using Auth.Application.Verification.Commands.ConfirmPhoneVerification;
using Auth.Domain;
using Auth.Infrastructure.TwoFactor;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.Verification.Commands.ConfirmPhoneVerification;

internal sealed class ConfirmPhoneVerificationCommandHandler(
    AuthDbContext dbContext,
    IAuditContext auditContext,
    ILogger<ConfirmPhoneVerificationCommandHandler> logger) : IRequestHandler<ConfirmPhoneVerificationCommand>
{
    public async Task Handle(ConfirmPhoneVerificationCommand command, CancellationToken cancellationToken)
    {
        var challenge = await dbContext.TwoFactorChallenges
            .FirstOrDefaultAsync(
                x => x.Id == command.ChallengeId && x.Purpose == TwoFactorChallenge.PurposePhoneVerification,
                cancellationToken)
            ?? throw new AuthException(VerificationErrorCatalog.InvalidChallenge);

        if (challenge.IsExpired(DateTime.UtcNow))
            throw new AuthException(VerificationErrorCatalog.ChallengeExpired);

        if (!challenge.HasAttemptsRemaining())
            throw new AuthException(VerificationErrorCatalog.MaxAttemptsExceeded);

        if (challenge.IsUsed)
            throw new AuthException(VerificationErrorCatalog.InvalidChallenge);

        TwoFactorValidation.ValidateDeliveryStatusOrThrow(challenge, TwoFactorChallenge.PurposePhoneVerification);

        if (!TwoFactorOtpSecurity.VerifyOtp(command.Otp, challenge.OtpSalt, challenge.OtpHash))
        {
            challenge.RegisterFailedAttempt();
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new AuthException(VerificationErrorCatalog.InvalidOtp);
        }

        challenge.MarkVerified();

        var user = await dbContext.Users.FirstAsync(x => x.Id == challenge.UserId, cancellationToken);
        user.VerifyPhone();

        await dbContext.SaveChangesAsync(cancellationToken);

        auditContext.EntityId = user.Id;
        auditContext.Details = new Dictionary<string, object?>
        {
            ["channel"] = "Sms",
            ["result"] = "success"
        };

        logger.LogInformation(
            "Verification userId={UserId} operation={Operation} result={Result}",
            user.Id, "PHONE_VERIFICATION_CONFIRMED", "SUCCESS");
    }
}
