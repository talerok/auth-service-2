namespace Auth.Application;

public sealed record WorkspaceDto(Guid Id, string Name, string Code, string Description, bool IsSystem);
public sealed record CreateWorkspaceRequest(string Name, string Code, string Description, bool IsSystem = false);
public sealed record UpdateWorkspaceRequest(string Name, string Code, string Description);
public sealed record PatchWorkspaceRequest(string? Name, string? Code, string? Description);

public sealed record ExportWorkspaceDto(string Name, string Code, string Description);
public sealed record ImportWorkspaceItem(string Name, string Code, string Description);
public sealed record ImportWorkspacesResult(int Created, int Updated);
