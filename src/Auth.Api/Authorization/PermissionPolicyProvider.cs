using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Auth.Api;

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith("perm-in:", StringComparison.Ordinal))
        {
            return _PermInPolicy(policyName);
        }

        return base.GetPolicyAsync(policyName);
    }

    private Task<AuthorizationPolicy?> _PermInPolicy(string policyName)
    {
        // policyName = "perm-in:{workspace}:{domain}:{permission}"
        var rest = policyName["perm-in:".Length..];
        var firstColon = rest.IndexOf(':');
        if (firstColon == -1)
            return base.GetPolicyAsync(policyName);

        var workspaceCode = rest[..firstColon];
        var afterWorkspace = rest[(firstColon + 1)..];

        var lastColon = afterWorkspace.LastIndexOf(':');
        if (lastColon == -1)
            return base.GetPolicyAsync(policyName);

        var domain = afterWorkspace[..lastColon];
        var permission = afterWorkspace[(lastColon + 1)..];

        var req = new PermissionInRequirement(workspaceCode, domain, permission);

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(req)
            .Build();
        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
