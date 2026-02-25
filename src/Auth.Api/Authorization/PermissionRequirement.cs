using Microsoft.AspNetCore.Authorization;

namespace Auth.Api;

public sealed class PermissionRequirement(string permissionCode) : IAuthorizationRequirement
{
    public string PermissionCode { get; } = permissionCode;
}
