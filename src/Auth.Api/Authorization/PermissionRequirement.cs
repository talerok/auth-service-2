using Microsoft.AspNetCore.Authorization;

namespace Auth.Api;

public sealed class PermissionRequirement(string permissionCode, bool inWorkspace) : IAuthorizationRequirement
{
    public string PermissionCode { get; } = permissionCode;
    public bool InWorkspace { get; } = inWorkspace;
}
