using Microsoft.AspNetCore.Authorization;

namespace Auth.Api;

public sealed class HasPermissionInAttribute(string workspaceCode, string permissionPattern)
    : AuthorizeAttribute($"perm-in:{workspaceCode}:{permissionPattern}");
