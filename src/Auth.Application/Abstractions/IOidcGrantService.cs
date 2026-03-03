using System.Security.Claims;
using Auth.Domain;

namespace Auth.Application;

public interface IOidcGrantService
{
    Task<PasswordGrantResult> HandlePasswordGrantAsync(
        string username, string password, IReadOnlyCollection<string> scopes, CancellationToken cancellationToken);

    Task<ClaimsPrincipal> HandleMfaOtpGrantAsync(
        Guid challengeId, TwoFactorChannel channel, string otp,
        IReadOnlyCollection<string> scopes, CancellationToken cancellationToken);

    Task<ClaimsPrincipal> BuildPrincipalAsync(
        Guid userId, IEnumerable<string> scopes, CancellationToken cancellationToken);

    Task<ClaimsPrincipal> HandleClientCredentialsGrantAsync(
        string clientId, IReadOnlyCollection<string> scopes, CancellationToken cancellationToken);
}
