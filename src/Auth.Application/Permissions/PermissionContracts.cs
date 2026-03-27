using Auth.Application.Common;

namespace Auth.Application;

public sealed record PermissionDto(Guid Id, string Domain, int Bit, string Code, string Description, bool IsSystem);
public sealed record CreatePermissionRequest(string Domain, string Code, string Description);
public sealed record UpdatePermissionRequest(string Code, string Description);
public sealed record PatchPermissionRequest(Optional<string> Code, Optional<string> Description);

public sealed record ExportPermissionDto(string Domain, int Bit, string Code, string Description);
public sealed record ImportPermissionItem(string Domain, int Bit, string Code, string Description);
public sealed record ImportPermissionsRequest(IReadOnlyCollection<ImportPermissionItem> Items);
public sealed record ImportPermissionsResult(int Created, int Updated, int Skipped);
