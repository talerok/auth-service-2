using Auth.Application.Common;

namespace Auth.Application;

public sealed record ServiceAccountDto(
    Guid Id,
    string Name,
    string Description,
    string ClientId,
    bool IsActive,
    IReadOnlyCollection<string> Audiences,
    int? AccessTokenLifetimeMinutes);

public sealed record CreateServiceAccountRequest(
    string Name,
    string Description,
    bool IsActive = true,
    IReadOnlyCollection<string>? Audiences = null,
    int? AccessTokenLifetimeMinutes = null);

public sealed record CreateServiceAccountResponse(
    ServiceAccountDto ServiceAccount,
    string ClientSecret);

public sealed record UpdateServiceAccountRequest(
    string Name,
    string Description,
    bool IsActive,
    IReadOnlyCollection<string>? Audiences = null,
    int? AccessTokenLifetimeMinutes = null);

public sealed record PatchServiceAccountRequest(
    Optional<string> Name,
    Optional<string> Description,
    Optional<bool> IsActive,
    Optional<IReadOnlyCollection<string>> Audiences,
    Optional<int?> AccessTokenLifetimeMinutes);

public sealed record RegenerateServiceAccountSecretResponse(string ClientSecret);

public sealed record ServiceAccountWorkspaceRolesItem(
    Guid WorkspaceId,
    IReadOnlyCollection<Guid> RoleIds);

public sealed record SetServiceAccountWorkspacesRequest(
    IReadOnlyCollection<ServiceAccountWorkspaceRolesItem> Workspaces);
