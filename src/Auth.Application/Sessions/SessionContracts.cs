namespace Auth.Application.Sessions;

public sealed record UserSessionResponse(
    Guid Id,
    Guid UserId,
    Guid? ApplicationId,
    string? ApplicationName,
    string IpAddress,
    string UserAgent,
    string AuthMethod,
    bool IsRevoked,
    bool IsCurrent,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime LastActivityAt,
    DateTime? RevokedAt,
    string? RevokedReason);
