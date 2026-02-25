using Microsoft.AspNetCore.Authorization;

namespace Auth.Api;

public sealed class PermissionInRequirement(string workspaceCode, string permission) : IAuthorizationRequirement
{
    public string WorkspaceCode { get; } = workspaceCode;
    public string Permission { get; } = permission;
}
