using Microsoft.AspNetCore.Authorization;

namespace Auth.Api;

public class HasPermissionInAttribute(string workspaceCode, string domain, string permission)
    : AuthorizeAttribute($"perm-in:{workspaceCode}:{domain}:{permission}");

public sealed class HasSystemPermissionAttribute(string permission)
    : HasPermissionInAttribute("system", "system", permission);
