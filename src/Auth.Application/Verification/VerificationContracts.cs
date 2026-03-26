namespace Auth.Application.Verification;

public sealed record SendVerificationResponse(Guid ChallengeId, DateTime ExpiresAt);
public sealed record ConfirmVerificationRequest(Guid ChallengeId, string Otp);
