using Auth.Domain;

namespace Auth.Application;

public sealed record ForcedPasswordChangeRequest(Guid ChallengeId, string NewPassword);
public sealed record RegisterRequest(string Username, string FullName, string Email, string Password);
public sealed record EnableTwoFactorRequest(TwoFactorChannel Channel, bool IsHighRisk = false);
public sealed record EnableTwoFactorResponse(Guid ChallengeId, TwoFactorChannel Channel, DateTime ExpiresAt);
public sealed record VerifyTwoFactorRequest(Guid ChallengeId, TwoFactorChannel Channel, string Otp);
