using System.Security.Claims;
using Auth.Domain;

namespace Auth.Application;

public abstract record PasswordGrantResult
{
    private PasswordGrantResult() { }

    public sealed record Success(ClaimsPrincipal Principal) : PasswordGrantResult;

    public sealed record MfaRequired(Guid ChallengeId, TwoFactorChannel Channel) : PasswordGrantResult;

    public sealed record PasswordChangeRequired(Guid ChallengeId) : PasswordGrantResult;
}
