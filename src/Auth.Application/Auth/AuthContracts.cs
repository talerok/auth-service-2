using Auth.Domain;

namespace Auth.Application;

public sealed record AuthTokensResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
public sealed record LoginRequest(string Username, string Password);
public sealed record LoginResponse(
    bool RequiresTwoFactor,
    AuthTokensResponse? Tokens,
    Guid? ChallengeId,
    TwoFactorChannel? Channel,
    bool RequiresPasswordChange = false,
    Guid? PasswordChangeChallengeId = null);
public sealed record ForcedPasswordChangeRequest(Guid ChallengeId, string NewPassword);
public sealed record RefreshRequest(string RefreshToken);
public sealed record RegisterRequest(string Username, string FullName, string Email, string Password);
public sealed record RevokeRequest(string RefreshToken);
public sealed record EnableTwoFactorRequest(TwoFactorChannel Channel, bool IsHighRisk = false);
public sealed record EnableTwoFactorResponse(Guid ChallengeId, TwoFactorChannel Channel, DateTime ExpiresAt);
public sealed record VerifyTwoFactorRequest(Guid ChallengeId, TwoFactorChannel Channel, string Otp);
