using Auth.Application;
using Auth.Domain;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.TwoFactor;

internal static class TwoFactorValidation
{
    public static void ValidateChannelOrThrow(TwoFactorChannel channel)
    {
        if (channel is not (TwoFactorChannel.Email or TwoFactorChannel.Sms))
        {
            throw new AuthException(TwoFactorErrorCatalog.UnsupportedChannel);
        }
    }

    public static void ValidateChallengeOrThrow(TwoFactorChallenge? challenge, TwoFactorChannel channel)
    {
        if (challenge is null)
        {
            throw new AuthException(TwoFactorErrorCatalog.ChallengeNotFound);
        }

        if (challenge.Channel != channel)
        {
            throw new AuthException(TwoFactorErrorCatalog.UnsupportedChannel);
        }

        if (challenge.IsExpired(DateTime.UtcNow))
        {
            throw new AuthException(TwoFactorErrorCatalog.ChallengeExpired);
        }

        if (!challenge.HasAttemptsRemaining())
        {
            throw new AuthException(TwoFactorErrorCatalog.AttemptsExceeded);
        }

        if (challenge.IsUsed)
        {
            throw new AuthException(TwoFactorErrorCatalog.OtpAlreadyUsed);
        }
    }

    public static void ValidateDeliveryStatusOrThrow(TwoFactorChallenge challenge, string purpose)
    {
        if (challenge.DeliveryStatus == TwoFactorChallenge.DeliveryPending)
        {
            var errorCode = purpose == TwoFactorChallenge.PurposeActivation
                ? TwoFactorErrorCatalog.ActivationNotCompleted
                : TwoFactorErrorCatalog.Required;
            throw new AuthException(errorCode);
        }

        if (challenge.DeliveryStatus == TwoFactorChallenge.DeliveryFailed)
        {
            throw new AuthException(TwoFactorErrorCatalog.DeliveryFailed);
        }

        if (challenge.DeliveryStatus == TwoFactorChallenge.ProviderUnavailable)
        {
            throw new AuthException(TwoFactorErrorCatalog.ProviderUnavailable);
        }
    }

    public static async Task VerifyOtpOrThrowAsync(
        TwoFactorChallenge challenge, string otp, AuthDbContext dbContext, ILogger logger, CancellationToken cancellationToken)
    {
        if (!TwoFactorOtpSecurity.VerifyOtp(otp, challenge.OtpSalt, challenge.OtpHash))
        {
            challenge.RegisterFailedAttempt();
            logger.LogInformation(
                "TwoFactorOperation userId={UserId} operation={Operation} result={Result}",
                challenge.UserId,
                "OTP_VERIFY",
                "FAILED");
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new AuthException(TwoFactorErrorCatalog.VerificationFailed);
        }
    }
}
