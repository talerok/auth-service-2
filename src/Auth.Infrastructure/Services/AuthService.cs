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
    IJwtTokenFactory jwtTokenFactory,
    ISearchIndexService searchIndexService,
    IOptions<IntegrationOptions> options,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly JwtOptions _jwt = options.Value.Jwt;
    private readonly TwoFactorOptions _twoFactor = options.Value.TwoFactor;
    private readonly PasswordChangeOptions _passwordChange = options.Value.PasswordChange;
    private readonly string _twoFactorKeyMaterial = string.IsNullOrWhiteSpace(options.Value.TwoFactor.EncryptionKey)
        ? options.Value.Jwt.Secret
        : options.Value.TwoFactor.EncryptionKey;

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Username == request.Username, cancellationToken);

        if (user is null || !user.IsActive || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new AuthException(AuthErrorCatalog.InvalidCredentials);
        }

        if (user.MustChangePassword)
        {
            var passwordChallenge = PasswordChangeChallenge.Create(
                user.Id,
                DateTime.UtcNow.AddMinutes(_passwordChange.PasswordChangeTtlMinutes));
            dbContext.PasswordChangeChallenges.Add(passwordChallenge);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "PasswordChangeOperation userId={UserId} operation={Operation} result={Result}",
                user.Id, "PASSWORD_CHANGE_CHALLENGE_CREATED", "SUCCESS");

            return new LoginResponse(false, null, null, null, true, passwordChallenge.Id);
        }

        if (!user.TwoFactorEnabled)
        {
            var masks = await BuildWorkspaceMasksAsync(user.Id, cancellationToken);
            var tokens = jwtTokenFactory.CreateTokens(user, masks);
            await SaveRefreshTokenAsync(user.Id, tokens.RefreshToken, cancellationToken);
            return new LoginResponse(false, tokens, null, null);
        }

        ValidateChannelOrThrow(user.TwoFactorChannel ?? TwoFactorChannel.Email);
        var challenge = await CreateLoginChallengeAsync(
            user.Id,
            user.TwoFactorChannel ?? TwoFactorChannel.Email,
            cancellationToken);
        logger.LogInformation(
            "TwoFactorOperation userId={UserId} operation={Operation} result={Result}",
            user.Id,
            "LOGIN_CHALLENGE_INITIATED",
            "SUCCESS");

        return new LoginResponse(true, null, challenge.Id, challenge.Channel);
    }

    public async Task<AuthTokensResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var current = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken, cancellationToken);
        if (current is null || current.RevokedAt is not null || current.ExpiresAt <= DateTime.UtcNow)
        {
            throw new AuthException(AuthErrorCatalog.InvalidRefreshToken);
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == current.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new AuthException(AuthErrorCatalog.UserInactive);
        }

        current.RevokedAt = DateTime.UtcNow;
        var masks = await BuildWorkspaceMasksAsync(user.Id, cancellationToken);
        var tokens = jwtTokenFactory.CreateTokens(user, masks);
        await SaveRefreshTokenAsync(user.Id, tokens.RefreshToken, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return tokens;
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
            Email = request.Email,
            PasswordHash = passwordHasher.Hash(request.Password),
            IsActive = true
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new UserDto(user.Id, user.Username, user.Email, user.Phone, user.IsActive, user.MustChangePassword, user.TwoFactorEnabled, user.TwoFactorChannel);
        await searchIndexService.IndexUserAsync(dto, cancellationToken);
        return dto;
    }

    public async Task RevokeAsync(RevokeRequest request, CancellationToken cancellationToken)
    {
        var refreshToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken, cancellationToken);
        if (refreshToken is null)
        {
            return;
        }

        refreshToken.RevokedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthTokensResponse> ForcedChangePasswordAsync(
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

        var masks = await BuildWorkspaceMasksAsync(user.Id, cancellationToken);
        var tokens = jwtTokenFactory.CreateTokens(user, masks);
        await SaveRefreshTokenAsync(user.Id, tokens.RefreshToken, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "PasswordChangeOperation userId={UserId} operation={Operation} result={Result}",
            user.Id, "FORCED_PASSWORD_CHANGED", "SUCCESS");

        return tokens;
    }

    private async Task SaveRefreshTokenAsync(Guid userId, string refreshToken, CancellationToken cancellationToken)
    {
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpirationDays)
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<TwoFactorChallenge> CreateLoginChallengeAsync(
        Guid userId,
        TwoFactorChannel channel,
        CancellationToken cancellationToken)
    {
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
        if (channel != TwoFactorChannel.Email)
        {
            throw new AuthException(TwoFactorErrorCatalog.UnsupportedChannel);
        }
    }

    private async Task<Dictionary<string, byte[]>> BuildWorkspaceMasksAsync(Guid userId, CancellationToken cancellationToken)
    {
        var matrix = await dbContext.UserWorkspaces
            .Where(uw => uw.UserId == userId)
            .Select(uw => new
            {
                uw.Workspace!.Code,
                Bits = uw.UserWorkspaceRoles
                    .SelectMany(uwr => uwr.Role!.RolePermissions)
                    .Select(rp => rp.Permission!.Bit)
            })
            .ToListAsync(cancellationToken);

        var result = new Dictionary<string, byte[]>();
        foreach (var row in matrix)
        {
            result[row.Code] = PermissionBitmask.BuildMask(row.Bits);
        }

        return result;
    }
}
