namespace Auth.Application;

public sealed record PermissionDto(Guid Id, int Bit, string Code, string Description, bool IsSystem);
public sealed record CreatePermissionRequest(string Code, string Description);
public sealed record UpdatePermissionRequest(string Description);
public sealed record PatchPermissionRequest(string? Description);
