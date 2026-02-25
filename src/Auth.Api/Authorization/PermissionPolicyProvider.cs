using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Auth.Api;

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith("perm:", StringComparison.Ordinal))
        {
            var permission = policyName["perm:".Length..];
            var policy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes("Bearer")
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
        return base.GetPolicyAsync(policyName);
    }
}
