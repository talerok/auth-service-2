using Microsoft.AspNetCore.Authorization;

namespace Auth.Api;

public sealed class HasPermissionInAttribute(string workspaceCode, string domain, string permission)
    : AuthorizeAttribute($"perm-in:{workspaceCode}:{domain}:{permission}");
