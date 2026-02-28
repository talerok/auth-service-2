using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using OpenIddict.Validation.AspNetCore;

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
        var rest = policyName["perm-in:".Length..];
        var colonIdx = rest.IndexOf(':');
        if (colonIdx == -1)
        {
            return base.GetPolicyAsync(policyName);
        }

        var code = rest[..colonIdx];
        var permission = rest[(colonIdx + 1)..];
        var req = new PermissionInRequirement(code, permission);

        var policy = new AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .AddRequirements(req)
            .Build();
        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
