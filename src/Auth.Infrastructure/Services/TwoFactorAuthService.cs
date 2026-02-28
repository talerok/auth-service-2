using Auth.Application;
using Auth.Domain;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure;

public sealed class TwoFactorAuthService(
    AuthDbContext dbContext,
    IOptions<IntegrationOptions> options,
    ILogger<TwoFactorAuthService> logger) : ITwoFactorAuthService
{
    private readonly TwoFactorOptions _twoFactor = options.Value.TwoFactor;
    private readonly string _twoFactorKeyMaterial = string.IsNullOrWhiteSpace(options.Value.TwoFactor.EncryptionKey)
        ? options.Value.Jwt.Secret
        : options.Value.TwoFactor.EncryptionKey;

    public async Task<EnableTwoFactorResponse> EnableTwoFactorAsync(
        Guid userId,
        EnableTwoFactorRequest request,
        CancellationToken cancellationToken)
    {
        ValidateChannelOrThrow(request.Channel);
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.UserNotFound);

        if (request.Channel == TwoFactorChannel.Sms && string.IsNullOrWhiteSpace(user.Phone))
        {
            throw new AuthException(TwoFactorErrorCatalog.PhoneRequired);
        }

        var challenge = await CreateActivationChallengeAsync(
            user.Id,
            request.Channel,
            request.IsHighRisk,
            cancellationToken);

        logger.LogInformation(
            "TwoFactorOperation userId={UserId} operation={Operation} result={Result}",
            user.Id,
            "ACTIVATION_INITIATED",
            "SUCCESS");

        return new EnableTwoFactorResponse(challenge.Id, challenge.Channel, challenge.ExpiresAt);
    }

    public async Task ConfirmTwoFactorActivationAsync(Guid userId, VerifyTwoFactorRequest request, CancellationToken cancellationToken)
    {
        ValidateChannelOrThrow(request.Channel);
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.UserNotFound);
        var challenge = await dbContext.TwoFactorChallenges
            .FirstOrDefaultAsync(
                x => x.Id == request.ChallengeId && x.UserId == user.Id && x.Purpose == TwoFactorChallenge.PurposeActivation,
                cancellationToken);

        ValidateChallengeOrThrow(challenge, request.Channel);
        ValidateDeliveryStatusOrThrow(challenge!, TwoFactorChallenge.PurposeActivation);
        await VerifyOtpOrThrowAsync(challenge!, request.Otp, cancellationToken);

        user.EnableTwoFactor(request.Channel);
        challenge!.MarkVerified();
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "TwoFactorOperation userId={UserId} operation={Operation} result={Result}",
            user.Id,
            "ACTIVATION_CONFIRMED",
            "SUCCESS");
    }

    public async Task DisableTwoFactorAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.UserNotFound);
        user.DisableTwoFactor();
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "TwoFactorOperation userId={UserId} operation={Operation} result={Result}",
            user.Id,
            "DISABLED",
            "SUCCESS");
    }

    public async Task<User> ValidateLoginOtpAsync(
        Guid challengeId,
        TwoFactorChannel channel,
        string otp,
        CancellationToken cancellationToken)
    {
        ValidateChannelOrThrow(channel);
        var challenge = await dbContext.TwoFactorChallenges
            .FirstOrDefaultAsync(
                x => x.Id == challengeId && x.Purpose == TwoFactorChallenge.PurposeLogin,
                cancellationToken);
        ValidateChallengeOrThrow(challenge, channel);
        ValidateDeliveryStatusOrThrow(challenge!, TwoFactorChallenge.PurposeLogin);
        await VerifyOtpOrThrowAsync(challenge!, otp, cancellationToken);

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

    private async Task<TwoFactorChallenge> CreateActivationChallengeAsync(
        Guid userId,
        TwoFactorChannel channel,
        bool isHighRisk,
        CancellationToken cancellationToken)
    {
        var ttl = isHighRisk ? _twoFactor.HighRiskOtpTtlMinutes : _twoFactor.StandardOtpTtlMinutes;
        var otp = CreateOtp();
        var otpSalt = TwoFactorOtpSecurity.CreateSalt();
        var otpHash = TwoFactorOtpSecurity.HashOtp(otp, otpSalt);
        var otpEncrypted = TwoFactorOtpSecurity.EncryptOtp(otp, _twoFactorKeyMaterial);

        var challenge = TwoFactorChallenge.Create(
            userId,
            TwoFactorChallenge.PurposeActivation,
            channel,
            otpHash,
            otpSalt,
            otpEncrypted,
            DateTime.UtcNow.AddMinutes(ttl),
            _twoFactor.MaxAttemptsPerChallenge);

        dbContext.TwoFactorChallenges.Add(challenge);
        await dbContext.SaveChangesAsync(cancellationToken);
        return challenge;
    }

    private string CreateOtp()
    {
        if (!string.IsNullOrWhiteSpace(_twoFactor.StaticOtpForTesting))
        {
            return _twoFactor.StaticOtpForTesting;
        }

        var minValue = (int)Math.Pow(10, _twoFactor.OtpLength - 1);
        var maxValueExclusive = (int)Math.Pow(10, _twoFactor.OtpLength);
        var value = RandomNumberGenerator.GetInt32(minValue, maxValueExclusive);
        return value.ToString();
    }

    private static void ValidateChannelOrThrow(TwoFactorChannel channel)
    {
        if (channel is not (TwoFactorChannel.Email or TwoFactorChannel.Sms))
        {
            throw new AuthException(TwoFactorErrorCatalog.UnsupportedChannel);
        }
    }

    private static void ValidateChallengeOrThrow(TwoFactorChallenge? challenge, TwoFactorChannel channel)
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

    private static void ValidateDeliveryStatusOrThrow(TwoFactorChallenge challenge, string purpose)
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

    private async Task VerifyOtpOrThrowAsync(TwoFactorChallenge challenge, string otp, CancellationToken cancellationToken)
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
