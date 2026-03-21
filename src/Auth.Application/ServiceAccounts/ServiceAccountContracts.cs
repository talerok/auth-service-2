namespace Auth.Application;

public sealed record ServiceAccountDto(
    Guid Id,
    string Name,
    string Description,
    string ClientId,
    bool IsActive);

public sealed record CreateServiceAccountRequest(
    string Name,
    string Description,
    bool IsActive = true);

public sealed record CreateServiceAccountResponse(
    ServiceAccountDto ServiceAccount,
    string ClientSecret);

public sealed record UpdateServiceAccountRequest(
    string Name,
    string Description,
    bool IsActive);

public sealed record PatchServiceAccountRequest(
    string? Name,
    string? Description,
    bool? IsActive);

public sealed record RegenerateServiceAccountSecretResponse(string ClientSecret);

public sealed record ServiceAccountWorkspaceRolesItem(
    Guid WorkspaceId,
    IReadOnlyCollection<Guid> RoleIds);

public sealed record SetServiceAccountWorkspacesRequest(
    IReadOnlyCollection<ServiceAccountWorkspaceRolesItem> Workspaces);
