using System.Security.Cryptography;
using Auth.Application;
using Auth.Application.Auth.Commands.CreateLoginChallenge;
using Auth.Domain;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Authentication.Commands.CreateLoginChallenge;

internal sealed class CreateLoginChallengeCommandHandler(
    AuthDbContext dbContext,
    IOptions<IntegrationOptions> options,
    ILogger<CreateLoginChallengeCommandHandler> logger) : IRequestHandler<CreateLoginChallengeCommand, TwoFactorChallenge>
{
    private readonly TwoFactorOptions _twoFactor = options.Value.TwoFactor;
    private readonly string _twoFactorKeyMaterial = options.Value.EncryptionKey;

    public async Task<TwoFactorChallenge> Handle(CreateLoginChallengeCommand command, CancellationToken cancellationToken)
    {
        ValidateChannelOrThrow(command.Channel);
        var otp = CreateOtp();
        var otpSalt = TwoFactorOtpSecurity.CreateSalt();
        var otpHash = TwoFactorOtpSecurity.HashOtp(otp, otpSalt);
        var otpEncrypted = TwoFactorOtpSecurity.EncryptOtp(otp, _twoFactorKeyMaterial);
        var challenge = TwoFactorChallenge.Create(
            command.UserId,
            TwoFactorChallenge.PurposeLogin,
            command.Channel,
            otpHash,
            otpSalt,
            otpEncrypted,
            DateTime.UtcNow.AddMinutes(_twoFactor.StandardOtpTtlMinutes),
            _twoFactor.MaxAttemptsPerChallenge);

        dbContext.TwoFactorChallenges.Add(challenge);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "TwoFactorOperation userId={UserId} operation={Operation} result={Result}",
            command.UserId, "LOGIN_CHALLENGE_INITIATED", "SUCCESS");

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
}
