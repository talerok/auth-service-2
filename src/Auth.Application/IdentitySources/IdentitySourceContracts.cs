using Auth.Domain;

namespace Auth.Application;

public sealed record IdentitySourceDto(
    Guid Id,
    string Name,
    string DisplayName,
    IdentitySourceType Type,
    bool IsEnabled,
    DateTime CreatedAt);

public sealed record IdentitySourceDetailDto(
    Guid Id,
    string Name,
    string DisplayName,
    IdentitySourceType Type,
    bool IsEnabled,
    DateTime CreatedAt,
    IdentitySourceOidcConfigDto? OidcConfig);

public sealed record IdentitySourceOidcConfigDto(
    string Authority,
    string ClientId,
    bool HasClientSecret);

public sealed record CreateIdentitySourceRequest(
    string Name,
    string DisplayName,
    IdentitySourceType Type,
    CreateOidcConfigRequest? OidcConfig = null);

public sealed record UpdateIdentitySourceRequest(
    string DisplayName,
    bool IsEnabled,
    CreateOidcConfigRequest? OidcConfig = null);

public sealed record CreateOidcConfigRequest(
    string Authority,
    string ClientId,
    string? ClientSecret = null);

public sealed record IdentitySourceLinkDto(
    Guid Id,
    Guid UserId,
    Guid IdentitySourceId,
    string ExternalIdentity,
    DateTime CreatedAt);

public sealed record CreateIdentitySourceLinkRequest(
    Guid UserId,
    string ExternalIdentity);

public sealed record TokenExchangeRequest(
    string IdentitySource,
    string Token);
