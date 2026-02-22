using Microsoft.AspNetCore.Authorization;

namespace Auth.Api;

public sealed class HasPermissionAttribute(string permissionCode) : AuthorizeAttribute($"perm:{permissionCode}");
public sealed class HasPermissionInWorkspaceAttribute(string permissionCode) : AuthorizeAttribute($"permws:{permissionCode}");
