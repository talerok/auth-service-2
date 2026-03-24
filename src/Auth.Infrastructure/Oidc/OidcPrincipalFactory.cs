using System.Security.Claims;
using System.Text.Json;
using Auth.Application;
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
        User user, IEnumerable<string> scopes, ISender sender, CancellationToken cancellationToken)
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

        var allMasks = await ResolveWorkspaceMasksAsync(
            OidcConstants.ExtractWorkspaceCodes(scopeList),
            userId => sender.Send(new BuildWorkspaceMasksQuery(userId), cancellationToken),
            user.Id);
        ApplyWorkspaceClaims(identity, allMasks);

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(scopeList);
        ApplyWorkspaceAudiences(principal, allMasks);

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

    /// <summary>
    /// Resolves workspace permission masks filtered to the requested workspace codes.
    /// Returns encoded masks ready for JWT serialization.
    /// </summary>
    internal static async Task<Dictionary<string, Dictionary<string, string>>> ResolveWorkspaceMasksAsync<TId>(
        IReadOnlyList<string> workspaceCodes,
        Func<TId, Task<Dictionary<string, Dictionary<string, byte[]>>>> loadMasks,
        TId subjectId)
    {
        if (workspaceCodes.Count == 0)
            return [];

        var allMasks = await loadMasks(subjectId);
        return allMasks
            .Where(kv => workspaceCodes.Contains(kv.Key))
            .ToDictionary(
                ws => ws.Key,
                ws => ws.Value.ToDictionary(d => d.Key, d => Convert.ToBase64String(d.Value)));
    }

    internal static void ApplyWorkspaceClaims(
        ClaimsIdentity identity, Dictionary<string, Dictionary<string, string>> accessibleMasks)
    {
        if (accessibleMasks.Count > 0)
            identity.AddClaim(new Claim("ws", JsonSerializer.Serialize(accessibleMasks)));
    }

    internal static void ApplyWorkspaceAudiences(
        ClaimsPrincipal principal, Dictionary<string, Dictionary<string, string>> accessibleMasks)
    {
        if (accessibleMasks.Count > 0)
            principal.SetAudiences(accessibleMasks.Keys.Select(c => $"ws:{c}").ToArray());
    }
}
