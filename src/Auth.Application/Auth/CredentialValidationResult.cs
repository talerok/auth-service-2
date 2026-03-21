using System.Security.Claims;
using Auth.Domain;

namespace Auth.Application;

public abstract record CredentialValidationResult
{
    private CredentialValidationResult() { }

    public sealed record Success(ClaimsPrincipal Principal) : CredentialValidationResult;

    public sealed record MfaRequired(Guid ChallengeId, TwoFactorChannel Channel) : CredentialValidationResult;

    public sealed record PasswordChangeRequired(Guid ChallengeId) : CredentialValidationResult;
}
