namespace Auth.Application;

public sealed record RoleDto(Guid Id, string Name, string Description);
public sealed record CreateRoleRequest(string Name, string Description);
public sealed record UpdateRoleRequest(string Name, string Description);
public sealed record PatchRoleRequest(string? Name, string? Description);

public sealed record ExportRoleDto(string Name, string Description, IReadOnlyCollection<string> Permissions);
public sealed record ImportRoleItem(string Name, string Description, IReadOnlyCollection<string> Permissions);
public sealed record ImportRolesResult(int Created, int Updated, int Skipped);
