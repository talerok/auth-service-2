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
    IdentitySourceOidcConfigDto? OidcConfig,
    IdentitySourceLdapConfigDto? LdapConfig = null);

public sealed record IdentitySourceOidcConfigDto(
    string Authority,
    string ClientId,
    bool HasClientSecret);

public sealed record IdentitySourceLdapConfigDto(
    string Host,
    int Port,
    string BaseDn,
    bool UseSsl);

public sealed record CreateIdentitySourceRequest(
    string Name,
    string DisplayName,
    IdentitySourceType Type,
    CreateOidcConfigRequest? OidcConfig = null,
    CreateLdapConfigRequest? LdapConfig = null);

public sealed record UpdateIdentitySourceRequest(
    string DisplayName,
    bool IsEnabled,
    CreateOidcConfigRequest? OidcConfig = null,
    CreateLdapConfigRequest? LdapConfig = null);

public sealed record CreateOidcConfigRequest(
    string Authority,
    string ClientId,
    string? ClientSecret = null);

public sealed record CreateLdapConfigRequest(
    string Host,
    int Port,
    string BaseDn,
    string BindDn,
    string? BindPassword = null,
    bool UseSsl = false,
    string SearchFilter = "(uid={username})");

public sealed record IdentitySourceLinkDto(
    Guid Id,
    Guid UserId,
    Guid IdentitySourceId,
    string ExternalIdentity,
    DateTime CreatedAt);

public sealed record UserIdentitySourceLinkDto(
    Guid LinkId,
    Guid IdentitySourceId,
    string IdentitySourceName,
    string IdentitySourceDisplayName,
    IdentitySourceType IdentitySourceType,
    string ExternalIdentity,
    DateTime CreatedAt);

public sealed record CreateIdentitySourceLinkRequest(
    Guid UserId,
    string ExternalIdentity);

public sealed record TokenExchangeRequest(
    string IdentitySource,
    string Token);
