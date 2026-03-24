namespace Auth.Domain;

public sealed class Application : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsConfidential { get; init; } = true;
    public string? LogoUrl { get; set; }
    public string? HomepageUrl { get; set; }

    public List<string> RedirectUris { get; private set; } = [];
    public List<string> PostLogoutRedirectUris { get; private set; } = [];
    public List<string> AllowedOrigins { get; private set; } = [];
    public List<string> Scopes { get; private set; } = [];
    public List<string> GrantTypes { get; private set; } = [];
    public List<string> Audiences { get; private set; } = [];

    public void SetRedirectUris(List<string> uris) => RedirectUris = uris;
    public void SetPostLogoutRedirectUris(List<string> uris) => PostLogoutRedirectUris = uris;
    public void SetAllowedOrigins(List<string> origins) => AllowedOrigins = origins;
    public void SetScopes(List<string> scopes) => Scopes = scopes;
    public void SetGrantTypes(List<string> grantTypes) => GrantTypes = grantTypes;
    public void SetAudiences(List<string> audiences) => Audiences = audiences;

    public int? AccessTokenLifetimeMinutes { get; set; }
    public int? RefreshTokenLifetimeMinutes { get; set; }
}
