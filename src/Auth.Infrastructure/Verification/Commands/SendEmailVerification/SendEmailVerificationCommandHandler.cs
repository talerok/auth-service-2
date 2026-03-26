using System.Security.Cryptography;
using Auth.Application;
using Auth.Application.Verification;
using Auth.Application.Verification.Commands.SendEmailVerification;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Verification.Commands.SendEmailVerification;

internal sealed class SendEmailVerificationCommandHandler(
    AuthDbContext dbContext,
    IOptions<IntegrationOptions> options,
    IAuditContext auditContext,
    ILogger<SendEmailVerificationCommandHandler> logger) : IRequestHandler<SendEmailVerificationCommand, SendVerificationResponse>
{
    private readonly TwoFactorOptions _twoFactor = options.Value.TwoFactor;
    private readonly string _encryptionKey = options.Value.EncryptionKey;

    public async Task<SendVerificationResponse> Handle(SendEmailVerificationCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.UserId, cancellationToken)
            ?? throw new AuthException(VerificationErrorCatalog.UserNotFound);

        if (string.IsNullOrWhiteSpace(user.Email))
            throw new AuthException(VerificationErrorCatalog.NoEmailConfigured);

        var hasActive = await dbContext.TwoFactorChallenges.AnyAsync(
            x => x.UserId == command.UserId
                 && x.Purpose == TwoFactorChallenge.PurposeEmailVerification
                 && !x.IsUsed
                 && x.ExpiresAt > DateTime.UtcNow,
            cancellationToken);

        if (hasActive)
            throw new AuthException(VerificationErrorCatalog.VerificationCooldown);

        var otp = CreateOtp();
        var otpSalt = TwoFactorOtpSecurity.CreateSalt();
        var otpHash = TwoFactorOtpSecurity.HashOtp(otp, otpSalt);
        var otpEncrypted = TwoFactorOtpSecurity.EncryptOtp(otp, _encryptionKey);

        var challenge = TwoFactorChallenge.Create(
            command.UserId,
            TwoFactorChallenge.PurposeEmailVerification,
            TwoFactorChannel.Email,
            otpHash, otpSalt, otpEncrypted,
            DateTime.UtcNow.AddMinutes(_twoFactor.StandardOtpTtlMinutes),
            _twoFactor.MaxAttemptsPerChallenge);

        dbContext.TwoFactorChallenges.Add(challenge);
        await dbContext.SaveChangesAsync(cancellationToken);

        auditContext.Details = new Dictionary<string, object?> { ["channel"] = "Email" };

        logger.LogInformation(
            "Verification userId={UserId} operation={Operation} result={Result}",
            user.Id, "EMAIL_VERIFICATION_INITIATED", "SUCCESS");

        return new SendVerificationResponse(challenge.Id, challenge.ExpiresAt);
    }

    private string CreateOtp()
    {
        if (!string.IsNullOrWhiteSpace(_twoFactor.StaticOtpForTesting))
            return _twoFactor.StaticOtpForTesting;

        var minValue = (int)Math.Pow(10, _twoFactor.OtpLength - 1);
        var maxValueExclusive = (int)Math.Pow(10, _twoFactor.OtpLength);
        return RandomNumberGenerator.GetInt32(minValue, maxValueExclusive).ToString();
    }
}
