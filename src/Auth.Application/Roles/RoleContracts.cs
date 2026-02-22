namespace Auth.Application;

public sealed record RoleDto(Guid Id, string Name, string Description);
public sealed record CreateRoleRequest(string Name, string Description);
public sealed record UpdateRoleRequest(string Name, string Description);
public sealed record PatchRoleRequest(string? Name, string? Description);
