using Auth.Application;
using Auth.Application.Verification;
using Auth.Application.Verification.Commands.ConfirmEmailVerification;
using Auth.Domain;
using Auth.Infrastructure.TwoFactor;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.Verification.Commands.ConfirmEmailVerification;

internal sealed class ConfirmEmailVerificationCommandHandler(
    AuthDbContext dbContext,
    IAuditContext auditContext,
    ILogger<ConfirmEmailVerificationCommandHandler> logger) : IRequestHandler<ConfirmEmailVerificationCommand>
{
    public async Task Handle(ConfirmEmailVerificationCommand command, CancellationToken cancellationToken)
    {
        var challenge = await dbContext.TwoFactorChallenges
            .FirstOrDefaultAsync(
                x => x.Id == command.ChallengeId && x.Purpose == TwoFactorChallenge.PurposeEmailVerification,
                cancellationToken)
            ?? throw new AuthException(VerificationErrorCatalog.InvalidChallenge);

        if (challenge.IsExpired(DateTime.UtcNow))
            throw new AuthException(VerificationErrorCatalog.ChallengeExpired);

        if (!challenge.HasAttemptsRemaining())
            throw new AuthException(VerificationErrorCatalog.MaxAttemptsExceeded);

        if (challenge.IsUsed)
            throw new AuthException(VerificationErrorCatalog.InvalidChallenge);

        TwoFactorValidation.ValidateDeliveryStatusOrThrow(challenge, TwoFactorChallenge.PurposeEmailVerification);

        if (!TwoFactorOtpSecurity.VerifyOtp(command.Otp, challenge.OtpSalt, challenge.OtpHash))
        {
            challenge.RegisterFailedAttempt();
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new AuthException(VerificationErrorCatalog.InvalidOtp);
        }

        challenge.MarkVerified();

        var user = await dbContext.Users.FirstAsync(x => x.Id == challenge.UserId, cancellationToken);
        user.VerifyEmail();

        await dbContext.SaveChangesAsync(cancellationToken);

        auditContext.EntityId = user.Id;
        auditContext.Details = new Dictionary<string, object?>
        {
            ["channel"] = "Email",
            ["result"] = "success"
        };

        logger.LogInformation(
            "Verification userId={UserId} operation={Operation} result={Result}",
            user.Id, "EMAIL_VERIFICATION_CONFIRMED", "SUCCESS");
    }
}
