namespace Auth.Application.Common;

public static class ScopeFilter
{
    private static readonly HashSet<string> PassthroughScopes = new(StringComparer.Ordinal)
    {
        "openid", "offline_access"
    };

    public static List<string> Filter(
        IReadOnlyCollection<string> requestedScopes,
        IReadOnlyCollection<string> allowedScopes)
    {
        if (allowedScopes.Count == 0)
            return [];

        var allowed = allowedScopes.ToHashSet(StringComparer.Ordinal);
        var wsWildcard = allowed.Contains(OidcConstants.WorkspaceWildcardScope);

        return requestedScopes.Where(s => IsPermitted(s, allowed, wsWildcard)).ToList();
    }

    private static bool IsPermitted(string scope, HashSet<string> allowed, bool wsWildcard)
    {
        if (PassthroughScopes.Contains(scope))
            return true;

        if (allowed.Contains(scope))
            return true;

        if (wsWildcard && scope.StartsWith(OidcConstants.WorkspaceScopePrefix, StringComparison.Ordinal))
            return true;

        return false;
    }
}
