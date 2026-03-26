using System.Security.Claims;
using System.Text.Json;
using Auth.Application;
using Auth.Application.Oidc.Queries.GetApplicationAudiences;
using Auth.Application.Workspaces.Queries.BuildWorkspaceMasks;
using Auth.Domain;
using MediatR;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.Infrastructure.Oidc;

internal static class OidcPrincipalFactory
{
    internal const string OidcServerScheme = "OpenIddict.Server.AspNetCore";

    public static async Task<ClaimsPrincipal> CreateUserPrincipalAsync(
        User user, IEnumerable<string> scopes, ISender sender, string? clientId,
        CancellationToken cancellationToken, IReadOnlyList<string>? authMethods = null)
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

        var isWildcard = OidcConstants.IsWildcardWorkspaceScope(scopeList);
        var workspaceCodes = OidcConstants.ExtractWorkspaceCodes(scopeList);
        var accessibleMasks = await ResolveWorkspaceMasksAsync(
            workspaceCodes,
            userId => sender.Send(new BuildWorkspaceMasksQuery(userId), cancellationToken),
            user.Id,
            isWildcard);
        ApplyWorkspaceClaims(identity, accessibleMasks);

        identity.SetClaim(Claims.AuthenticationTime,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

        if (authMethods is { Count: > 0 })
        {
            foreach (var method in authMethods)
                identity.AddClaim(new Claim(Claims.AuthenticationMethodReference, method));
        }

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(scopeList);
        await ApplyAudiencesAsync(principal, clientId, sender, cancellationToken);

        principal.SetDestinations(claim => claim.Type switch
        {
            Claims.Subject => [Destinations.AccessToken, Destinations.IdentityToken],
            Claims.Name => [Destinations.AccessToken, Destinations.IdentityToken],
            Claims.PreferredUsername => [Destinations.IdentityToken],
            Claims.Email => [Destinations.IdentityToken],
            Claims.PhoneNumber => [Destinations.IdentityToken],
            Claims.AuthenticationMethodReference => [Destinations.IdentityToken],
            Claims.AuthenticationTime => [Destinations.IdentityToken],
            _ when claim.Type.StartsWith(OidcConstants.WorkspaceScopePrefix, StringComparison.Ordinal)
                => [Destinations.AccessToken],
            _ => [Destinations.AccessToken]
        });

        return principal;
    }

    /// <summary>
    /// Resolves workspace permission masks filtered to the requested workspace codes.
    /// When <paramref name="wildcard"/> is true, returns all accessible workspaces without filtering.
    /// Returns encoded masks ready for JWT serialization.
    /// </summary>
    internal static async Task<Dictionary<string, Dictionary<string, string>>> ResolveWorkspaceMasksAsync<TId>(
        IReadOnlyList<string> workspaceCodes,
        Func<TId, Task<Dictionary<string, Dictionary<string, byte[]>>>> loadMasks,
        TId subjectId,
        bool wildcard = false)
    {
        if (!wildcard && workspaceCodes.Count == 0)
            return [];

        var allMasks = await loadMasks(subjectId);

        var filtered = wildcard
            ? allMasks
            : allMasks.Where(kv => workspaceCodes.Contains(kv.Key));

        return filtered.ToDictionary(
            ws => ws.Key,
            ws => ws.Value.ToDictionary(d => d.Key, d => Convert.ToBase64String(d.Value)));
    }

    internal static async Task ApplyAudiencesAsync(
        ClaimsPrincipal principal, string? clientId, ISender sender, CancellationToken cancellationToken)
    {
        if (clientId is null) return;

        var audiences = await sender.Send(new GetApplicationAudiencesQuery(clientId), cancellationToken);
        if (audiences.Count > 0)
            principal.SetResources(audiences);
    }

    internal static void ApplyWorkspaceClaims(
        ClaimsIdentity identity, Dictionary<string, Dictionary<string, string>> accessibleMasks)
    {
        foreach (var (code, domains) in accessibleMasks)
            identity.AddClaim(new Claim($"ws:{code}", JsonSerializer.Serialize(domains)));
    }
}
