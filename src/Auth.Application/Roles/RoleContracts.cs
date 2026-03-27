using Auth.Application.Common;

namespace Auth.Application;

public sealed record RoleDto(Guid Id, string Name, string Code, string Description);
public sealed record CreateRoleRequest(string Name, string Code, string Description);
public sealed record UpdateRoleRequest(string Name, string Code, string Description);
public sealed record PatchRoleRequest(Optional<string> Name, Optional<string> Code, Optional<string> Description);

public sealed record ExportRoleDto(string Name, string Code, string Description, IReadOnlyCollection<string> Permissions);
public sealed record ImportRoleItem(string Name, string Code, string Description, IReadOnlyCollection<string> Permissions);
public sealed record ImportRolesResult(int Created, int Updated, int Skipped);
