using Auth.Application;
using Auth.Domain;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure;

public sealed class AuthService(
    AuthDbContext dbContext,
    IPasswordHasher passwordHasher,
    ISearchIndexService searchIndexService,
    IOptions<IntegrationOptions> options,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly TwoFactorOptions _twoFactor = options.Value.TwoFactor;
    private readonly PasswordChangeOptions _passwordChange = options.Value.PasswordChange;
    private readonly string _twoFactorKeyMaterial = string.IsNullOrWhiteSpace(options.Value.TwoFactor.EncryptionKey)
        ? options.Value.Jwt.Secret
        : options.Value.TwoFactor.EncryptionKey;

    public async Task<User> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Username == username, cancellationToken);

        if (user is null || !user.IsActive || !passwordHasher.Verify(password, user.PasswordHash))
        {
            throw new AuthException(AuthErrorCatalog.InvalidCredentials);
        }

        return user;
    }

    public async Task<User> GetActiveUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null || !user.IsActive)
            throw new AuthException(AuthErrorCatalog.UserInactive);

        return user;
    }

    public async Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Users.AnyAsync(
            x => x.Username == request.Username || x.Email == request.Email, cancellationToken);
        if (exists)
        {
            throw new AuthException(AuthErrorCatalog.DuplicateIdentity);
        }

        var user = new User
        {
            Username = request.Username,
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = passwordHasher.Hash(request.Password),
            IsActive = true
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new UserDto(user.Id, user.Username, user.FullName, user.Email, user.Phone, user.IsActive, user.MustChangePassword, user.TwoFactorEnabled, user.TwoFactorChannel);
        await searchIndexService.IndexUserAsync(dto, cancellationToken);
        return dto;
    }

    public async Task<User> ValidateForcedPasswordChangeAsync(
        ForcedPasswordChangeRequest request, CancellationToken cancellationToken)
    {
        var passwordChallenge = await dbContext.PasswordChangeChallenges
            .FirstOrDefaultAsync(x => x.Id == request.ChallengeId, cancellationToken);

        if (passwordChallenge is null || passwordChallenge.IsUsed || passwordChallenge.IsExpired(DateTime.UtcNow))
            throw new AuthException(AuthErrorCatalog.InvalidPasswordChangeChallenge);

        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == passwordChallenge.UserId, cancellationToken);

        if (user is null || !user.IsActive)
            throw new AuthException(AuthErrorCatalog.UserInactive);

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.ClearMustChangePassword();
        passwordChallenge.MarkAsUsed();
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "PasswordChangeOperation userId={UserId} operation={Operation} result={Result}",
            user.Id, "FORCED_PASSWORD_CHANGED", "SUCCESS");

        return user;
    }

    public async Task<PasswordChangeChallenge> CreatePasswordChangeChallengeAsync(Guid userId, CancellationToken cancellationToken)
    {
        var challenge = PasswordChangeChallenge.Create(
            userId,
            DateTime.UtcNow.AddMinutes(_passwordChange.PasswordChangeTtlMinutes));

        dbContext.PasswordChangeChallenges.Add(challenge);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "PasswordChangeOperation userId={UserId} operation={Operation} result={Result}",
            userId, "CHALLENGE_CREATED", "SUCCESS");

        return challenge;
    }

    public async Task<TwoFactorChallenge> CreateLoginChallengeAsync(
        Guid userId,
        TwoFactorChannel channel,
        CancellationToken cancellationToken)
    {
        ValidateChannelOrThrow(channel);
        var otp = CreateOtp();
        var otpSalt = TwoFactorOtpSecurity.CreateSalt();
        var otpHash = TwoFactorOtpSecurity.HashOtp(otp, otpSalt);
        var otpEncrypted = TwoFactorOtpSecurity.EncryptOtp(otp, _twoFactorKeyMaterial);
        var challenge = TwoFactorChallenge.Create(
            userId,
            TwoFactorChallenge.PurposeLogin,
            channel,
            otpHash,
            otpSalt,
            otpEncrypted,
            DateTime.UtcNow.AddMinutes(_twoFactor.StandardOtpTtlMinutes),
            _twoFactor.MaxAttemptsPerChallenge);

        dbContext.TwoFactorChallenges.Add(challenge);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "TwoFactorOperation userId={UserId} operation={Operation} result={Result}",
            userId, "LOGIN_CHALLENGE_INITIATED", "SUCCESS");

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
