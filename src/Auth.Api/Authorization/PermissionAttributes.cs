using Microsoft.AspNetCore.Authorization;

namespace Auth.Api;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class HasPermissionInAttribute(string workspaceCode, string domain, string permission)
    : AuthorizeAttribute($"perm-in:{workspaceCode}:{domain}:{permission}");

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class HasSystemPermissionAttribute(string permission)
    : HasPermissionInAttribute("system", "system", permission);
