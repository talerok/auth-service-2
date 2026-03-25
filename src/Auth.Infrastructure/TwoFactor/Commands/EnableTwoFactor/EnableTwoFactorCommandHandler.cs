using System.Security.Cryptography;
using Auth.Application;
using Auth.Application.TwoFactor.Commands.EnableTwoFactor;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.TwoFactor.Commands.EnableTwoFactor;

internal sealed class EnableTwoFactorCommandHandler(
    AuthDbContext dbContext,
    IOptions<IntegrationOptions> options,
    IAuditContext auditContext,
    ILogger<EnableTwoFactorCommandHandler> logger) : IRequestHandler<EnableTwoFactorCommand, EnableTwoFactorResponse>
{
    private readonly TwoFactorOptions _twoFactor = options.Value.TwoFactor;
    private readonly string _twoFactorKeyMaterial = options.Value.EncryptionKey;

    public async Task<EnableTwoFactorResponse> Handle(EnableTwoFactorCommand command, CancellationToken cancellationToken)
    {
        TwoFactorValidation.ValidateChannelOrThrow(command.Channel);
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.UserId, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.UserNotFound);

        if (command.Channel == TwoFactorChannel.Sms && string.IsNullOrWhiteSpace(user.Phone))
        {
            throw new AuthException(TwoFactorErrorCatalog.PhoneRequired);
        }

        var ttl = command.IsHighRisk ? _twoFactor.HighRiskOtpTtlMinutes : _twoFactor.StandardOtpTtlMinutes;
        var otp = CreateOtp();
        var otpSalt = TwoFactorOtpSecurity.CreateSalt();
        var otpHash = TwoFactorOtpSecurity.HashOtp(otp, otpSalt);
        var otpEncrypted = TwoFactorOtpSecurity.EncryptOtp(otp, _twoFactorKeyMaterial);

        var challenge = TwoFactorChallenge.Create(
            command.UserId,
            TwoFactorChallenge.PurposeActivation,
            command.Channel,
            otpHash,
            otpSalt,
            otpEncrypted,
            DateTime.UtcNow.AddMinutes(ttl),
            _twoFactor.MaxAttemptsPerChallenge);

        dbContext.TwoFactorChallenges.Add(challenge);
        await dbContext.SaveChangesAsync(cancellationToken);

        auditContext.Details = new Dictionary<string, object?>
        {
            ["channel"] = command.Channel.ToString()
        };

        logger.LogInformation(
            "TwoFactorOperation userId={UserId} operation={Operation} result={Result}",
            user.Id,
            "ACTIVATION_INITIATED",
            "SUCCESS");

        return new EnableTwoFactorResponse(challenge.Id, challenge.Channel, challenge.ExpiresAt);
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
}
