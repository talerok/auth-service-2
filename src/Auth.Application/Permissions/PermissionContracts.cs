namespace Auth.Application;

public sealed record PermissionDto(Guid Id, int Bit, string Code, string Description, bool IsSystem);
public sealed record CreatePermissionRequest(string Code, string Description);
public sealed record UpdatePermissionRequest(string Code, string Description);
public sealed record PatchPermissionRequest(string? Code, string? Description);

public sealed record ExportPermissionDto(int Bit, string Code, string Description);
public sealed record ImportPermissionItem(int Bit, string Code, string Description);
public sealed record ImportPermissionsRequest(IReadOnlyCollection<ImportPermissionItem> Items);
public sealed record ImportPermissionsResult(int Created, int Updated);
