using Auth.Application.Common;

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
    IReadOnlyCollection<string> Audiences,
    int? AccessTokenLifetimeMinutes,
    int? RefreshTokenLifetimeMinutes,
    bool RequireEmailVerified,
    bool RequirePhoneVerified);

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
    List<string>? Audiences = null,
    int? AccessTokenLifetimeMinutes = null,
    int? RefreshTokenLifetimeMinutes = null,
    bool RequireEmailVerified = false,
    bool RequirePhoneVerified = false);

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
    List<string> Audiences,
    int? AccessTokenLifetimeMinutes,
    int? RefreshTokenLifetimeMinutes,
    bool RequireEmailVerified,
    bool RequirePhoneVerified);

public sealed record PatchApplicationRequest(
    Optional<string> Name,
    Optional<string> Description,
    Optional<bool> IsActive,
    Optional<string?> LogoUrl,
    Optional<string?> HomepageUrl,
    Optional<List<string>> RedirectUris,
    Optional<List<string>> PostLogoutRedirectUris,
    Optional<List<string>> AllowedOrigins,
    Optional<string> ConsentType,
    Optional<List<string>> Scopes,
    Optional<List<string>> GrantTypes,
    Optional<List<string>> Audiences,
    Optional<int?> AccessTokenLifetimeMinutes,
    Optional<int?> RefreshTokenLifetimeMinutes,
    Optional<bool> RequireEmailVerified,
    Optional<bool> RequirePhoneVerified);

public sealed record RegenerateApplicationSecretResponse(string ClientSecret);
