namespace Auth.Domain;

public sealed class Application : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsConfidential { get; set; } = true;
    public string? LogoUrl { get; set; }
    public string? HomepageUrl { get; set; }
    public List<string> RedirectUris { get; set; } = [];
    public List<string> PostLogoutRedirectUris { get; set; } = [];
    public List<string> AllowedOrigins { get; set; } = [];
    public List<string> Scopes { get; set; } = [];
    public List<string> GrantTypes { get; set; } = [];
    public int? AccessTokenLifetimeMinutes { get; set; }
    public int? RefreshTokenLifetimeMinutes { get; set; }
}
