using Microsoft.AspNetCore.Authorization;

namespace Auth.Api;

public sealed class PermissionInRequirement(string workspaceCode, string domain, string permission)
    : IAuthorizationRequirement
{
    public string WorkspaceCode { get; } = workspaceCode;
    public string Domain { get; } = domain;
    public string Permission { get; } = permission;
}
