namespace Auth.Application;

public sealed record ApiClientDto(
    Guid Id,
    string Name,
    string Description,
    string ClientId,
    bool IsActive);

public sealed record CreateApiClientRequest(
    string Name,
    string Description,
    bool IsActive = true);

public sealed record CreateApiClientResponse(
    ApiClientDto Client,
    string ClientSecret);

public sealed record UpdateApiClientRequest(
    string Name,
    string Description,
    bool IsActive);

public sealed record PatchApiClientRequest(
    string? Name,
    string? Description,
    bool? IsActive);

public sealed record RegenerateApiClientSecretResponse(string ClientSecret);

public sealed record ApiClientWorkspaceRolesItem(
    Guid WorkspaceId,
    IReadOnlyCollection<Guid> RoleIds);

public sealed record SetApiClientWorkspacesRequest(
    IReadOnlyCollection<ApiClientWorkspaceRolesItem> Workspaces);
