using Microsoft.AspNetCore.Authorization;

namespace Auth.Api;

public sealed class PermissionInRequirement(string workspaceCode, string permissionPattern) : IAuthorizationRequirement
{
    public string WorkspaceCode { get; } = workspaceCode;
    public string PermissionPattern { get; } = permissionPattern;
}
