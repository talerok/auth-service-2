namespace Auth.Application;

public sealed record ApplicationDto(
    Guid Id,
    string Name,
    string Description,
    string ClientId,
    bool IsActive,
    bool IsConfidential,
    string? LogoUrl,
    string? HomepageUrl,
    IReadOnlyCollection<string> RedirectUris,
    IReadOnlyCollection<string> PostLogoutRedirectUris,
    IReadOnlyCollection<string> AllowedOrigins,
    IReadOnlyCollection<string> Scopes,
    IReadOnlyCollection<string> GrantTypes,
    int? AccessTokenLifetimeMinutes,
    int? RefreshTokenLifetimeMinutes);

public sealed record CreateApplicationRequest(
    string Name,
    string Description,
    bool IsActive = true,
    bool IsConfidential = true,
    string? LogoUrl = null,
    string? HomepageUrl = null,
    List<string>? RedirectUris = null,
    List<string>? PostLogoutRedirectUris = null,
    List<string>? AllowedOrigins = null,
    string? ConsentType = null,
    List<string>? Scopes = null,
    List<string>? GrantTypes = null,
    int? AccessTokenLifetimeMinutes = null,
    int? RefreshTokenLifetimeMinutes = null);

public sealed record CreateApplicationResponse(
    ApplicationDto Application,
    string? ClientSecret);

public sealed record UpdateApplicationRequest(
    string Name,
    string Description,
    bool IsActive,
    string? LogoUrl,
    string? HomepageUrl,
    List<string> RedirectUris,
    List<string> PostLogoutRedirectUris,
    List<string> AllowedOrigins,
    string? ConsentType,
    List<string> Scopes,
    List<string> GrantTypes,
    int? AccessTokenLifetimeMinutes,
    int? RefreshTokenLifetimeMinutes);

public sealed record PatchApplicationRequest(
    string? Name,
    string? Description,
    bool? IsActive,
    string? LogoUrl,
    string? HomepageUrl,
    List<string>? RedirectUris,
    List<string>? PostLogoutRedirectUris,
    List<string>? AllowedOrigins,
    string? ConsentType,
    List<string>? Scopes,
    List<string>? GrantTypes,
    int? AccessTokenLifetimeMinutes,
    int? RefreshTokenLifetimeMinutes);

public sealed record RegenerateApplicationSecretResponse(string ClientSecret);
