using System.Security.Claims;
using System.Text.Json;
using Auth.Application;
using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.Infrastructure;

public sealed class OidcGrantService(
    IAuthService authService,
    ITwoFactorAuthService twoFactorAuthService,
    IWorkspaceMaskService workspaceMaskService,
    AuthDbContext dbContext) : IOidcGrantService
{
    private const string OidcServerScheme = "OpenIddict.Server.AspNetCore";

    public async Task<PasswordGrantResult> HandlePasswordGrantAsync(
        string username, string password, IReadOnlyCollection<string> scopes, CancellationToken cancellationToken)
    {
        var user = await authService.ValidateCredentialsAsync(username, password, cancellationToken);

        if (user.MustChangePassword)
        {
            var challenge = await authService.CreatePasswordChangeChallengeAsync(user.Id, cancellationToken);
            return new PasswordGrantResult.PasswordChangeRequired(challenge.Id);
        }

        if (user.TwoFactorEnabled)
        {
            var mfaChallenge = await authService.CreateLoginChallengeAsync(
                user.Id, user.TwoFactorChannel!.Value, cancellationToken);
            return new PasswordGrantResult.MfaRequired(mfaChallenge.Id, mfaChallenge.Channel);
        }

        var principal = await CreateOidcPrincipalAsync(user, scopes, cancellationToken);
        return new PasswordGrantResult.Success(principal);
    }

    public async Task<ClaimsPrincipal> HandleMfaOtpGrantAsync(
        Guid challengeId, TwoFactorChannel channel, string otp,
        IReadOnlyCollection<string> scopes, CancellationToken cancellationToken)
    {
        var user = await twoFactorAuthService.ValidateLoginOtpAsync(challengeId, channel, otp, cancellationToken);
        return await CreateOidcPrincipalAsync(user, scopes, cancellationToken);
    }

    public async Task<ClaimsPrincipal> BuildPrincipalAsync(
        Guid userId, IEnumerable<string> scopes, CancellationToken cancellationToken)
    {
        var user = await authService.GetActiveUserAsync(userId, cancellationToken);
        return await CreateOidcPrincipalAsync(user, scopes, cancellationToken);
    }

    public async Task<ClaimsPrincipal> HandleClientCredentialsGrantAsync(
        string clientId, IReadOnlyCollection<string> scopes, CancellationToken cancellationToken)
    {
        var apiClient = await dbContext.ApiClients
            .FirstOrDefaultAsync(x => x.ClientId == clientId, cancellationToken);

        if (apiClient is null)
            throw new AuthException(AuthErrorCatalog.ApiClientNotFound);

        if (!apiClient.IsActive)
            throw new AuthException(AuthErrorCatalog.ApiClientInactive);

        var scopeList = scopes.ToList();
        var identity = new ClaimsIdentity(OidcServerScheme, Claims.Name, Claims.Role);

        identity.SetClaim(Claims.Subject, apiClient.Id.ToString());
        identity.SetClaim(Claims.Name, apiClient.Name);
        identity.SetClaim(Claims.PreferredUsername, apiClient.ClientId);

        if (scopeList.Contains("ws"))
        {
            var masks = await workspaceMaskService.BuildApiClientWorkspaceMasksAsync(apiClient.Id, cancellationToken);
            var wsPayload = masks.ToDictionary(x => x.Key, x => Convert.ToBase64String(x.Value));
            identity.AddClaim(new Claim("ws", JsonSerializer.Serialize(wsPayload)));
        }

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(scopeList);

        principal.SetDestinations(claim => claim.Type switch
        {
            Claims.Subject => [Destinations.AccessToken, Destinations.IdentityToken],
            Claims.Name => [Destinations.IdentityToken],
            Claims.PreferredUsername => [Destinations.IdentityToken],
            "ws" => [Destinations.AccessToken],
            _ => [Destinations.AccessToken]
        });

        return principal;
    }

    private async Task<ClaimsPrincipal> CreateOidcPrincipalAsync(
        User user, IEnumerable<string> scopes, CancellationToken cancellationToken)
    {
        var scopeList = scopes.ToList();

        var identity = new ClaimsIdentity(OidcServerScheme, Claims.Name, Claims.Role);

        identity.SetClaim(Claims.Subject, user.Id.ToString());
        identity.SetClaim(Claims.Name, user.FullName);
        identity.SetClaim(Claims.PreferredUsername, user.Username);

        if (scopeList.Contains(Scopes.Email))
            identity.SetClaim(Claims.Email, user.Email);

        if (scopeList.Contains(Scopes.Phone) && !string.IsNullOrWhiteSpace(user.Phone))
            identity.SetClaim(Claims.PhoneNumber, user.Phone);

        if (scopeList.Contains("ws"))
        {
            var masks = await workspaceMaskService.BuildWorkspaceMasksAsync(user.Id, cancellationToken);
            var wsPayload = masks.ToDictionary(x => x.Key, x => Convert.ToBase64String(x.Value));
            identity.AddClaim(new Claim("ws", JsonSerializer.Serialize(wsPayload)));
        }

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(scopeList);

        principal.SetDestinations(claim => claim.Type switch
        {
            Claims.Subject => [Destinations.AccessToken, Destinations.IdentityToken],
            Claims.Name => [Destinations.IdentityToken],
            Claims.PreferredUsername => [Destinations.IdentityToken],
            Claims.Email => [Destinations.IdentityToken],
            Claims.PhoneNumber => [Destinations.IdentityToken],
            "ws" => [Destinations.AccessToken],
            _ => [Destinations.AccessToken]
        });

        return principal;
    }
}
