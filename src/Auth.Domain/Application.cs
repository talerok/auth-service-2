namespace Auth.Domain;

public sealed class Application : EntityBase
{
    [Auditable] public string Name { get; set; } = string.Empty;
    [Auditable] public string Description { get; set; } = string.Empty;
    [Auditable] public string ClientId { get; init; } = string.Empty;
    [Auditable] public bool IsActive { get; set; } = true;
    [Auditable] public bool IsConfidential { get; init; } = true;
    [Auditable] public string? LogoUrl { get; set; }
    [Auditable] public string? HomepageUrl { get; set; }

    [Auditable] public List<string> RedirectUris { get; private set; } = [];
    [Auditable] public List<string> PostLogoutRedirectUris { get; private set; } = [];
    [Auditable] public List<string> AllowedOrigins { get; private set; } = [];
    [Auditable] public List<string> Scopes { get; private set; } = [];
    [Auditable] public List<string> GrantTypes { get; private set; } = [];
    [Auditable] public List<string> Audiences { get; private set; } = [];

    public void SetRedirectUris(List<string> uris) => RedirectUris = uris;
    public void SetPostLogoutRedirectUris(List<string> uris) => PostLogoutRedirectUris = uris;
    public void SetAllowedOrigins(List<string> origins) => AllowedOrigins = origins;
    public void SetScopes(List<string> scopes) => Scopes = scopes;
    public void SetGrantTypes(List<string> grantTypes) => GrantTypes = grantTypes;
    public void SetAudiences(List<string> audiences) => Audiences = audiences;

    [Auditable] public int? AccessTokenLifetimeMinutes { get; set; }
    [Auditable] public int? RefreshTokenLifetimeMinutes { get; set; }
}
