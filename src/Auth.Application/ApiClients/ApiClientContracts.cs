using Auth.Domain;

namespace Auth.Application;

public sealed record ApiClientDto(
    Guid Id,
    string Name,
    string Description,
    string ClientId,
    bool IsActive,
    ApiClientType Type,
    bool IsConfidential,
    string? LogoUrl,
    string? HomepageUrl,
    IReadOnlyCollection<string> RedirectUris,
    IReadOnlyCollection<string> PostLogoutRedirectUris);

public sealed record CreateApiClientRequest(
    string Name,
    string Description,
    bool IsActive = true,
    ApiClientType Type = ApiClientType.ServiceAccount,
    bool IsConfidential = true,
    string? LogoUrl = null,
    string? HomepageUrl = null,
    List<string>? RedirectUris = null,
    List<string>? PostLogoutRedirectUris = null,
    string? ConsentType = null);

public sealed record CreateApiClientResponse(
    ApiClientDto Client,
    string? ClientSecret);

public sealed record UpdateApiClientRequest(
    string Name,
    string Description,
    bool IsActive,
    ApiClientType Type,
    bool IsConfidential,
    string? LogoUrl,
    string? HomepageUrl,
    List<string> RedirectUris,
    List<string> PostLogoutRedirectUris,
    string? ConsentType);

public sealed record PatchApiClientRequest(
    string? Name,
    string? Description,
    bool? IsActive,
    ApiClientType? Type,
    bool? IsConfidential,
    string? LogoUrl,
    string? HomepageUrl,
    List<string>? RedirectUris,
    List<string>? PostLogoutRedirectUris,
    string? ConsentType);

public sealed record RegenerateApiClientSecretResponse(string ClientSecret);

public sealed record ApiClientWorkspaceRolesItem(
    Guid WorkspaceId,
    IReadOnlyCollection<Guid> RoleIds);

public sealed record SetApiClientWorkspacesRequest(
    IReadOnlyCollection<ApiClientWorkspaceRolesItem> Workspaces);
