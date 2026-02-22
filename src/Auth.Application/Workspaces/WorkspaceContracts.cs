namespace Auth.Application;

public sealed record WorkspaceDto(Guid Id, string Name, string Description, bool IsSystem);
public sealed record CreateWorkspaceRequest(string Name, string Description, bool IsSystem = false);
public sealed record UpdateWorkspaceRequest(string Name, string Description);
public sealed record PatchWorkspaceRequest(string? Name, string? Description);
