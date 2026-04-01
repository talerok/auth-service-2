using System.Collections.Frozen;

namespace Auth.Application;

public static class OidcConstants
{
    public const string MfaOtpGrantType = "urn:custom:mfa_otp";
    public const string JwtBearerGrantType = "urn:ietf:params:oauth:grant-type:jwt-bearer";
    public const string LdapGrantType = "urn:custom:ldap";

    public const string WorkspaceScopePrefix = "ws:";
    public const string WorkspaceWildcardScope = "ws:*";

    public static readonly FrozenSet<string> AllowedGrantTypes = ((string[])
    [
        "authorization_code",
        "client_credentials",
        "refresh_token",
        "password",
        "jwt-bearer",
        "ldap",
        "mfa_otp"
    ]).ToFrozenSet(StringComparer.Ordinal);

    public static bool IsWildcardWorkspaceScope(IEnumerable<string> scopes)
        => scopes.Contains(WorkspaceWildcardScope);

    public static IReadOnlyList<string> ExtractWorkspaceCodes(IEnumerable<string> scopes)
        => scopes
            .Where(IsConcreteWorkspaceScope)
            .Select(s => s[WorkspaceScopePrefix.Length..])
            .ToList();

    private static bool IsConcreteWorkspaceScope(string scope)
        => scope != WorkspaceWildcardScope
           && scope.StartsWith(WorkspaceScopePrefix, StringComparison.Ordinal);
}
