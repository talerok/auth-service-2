using System.Text.Json;
using Auth.Application;
using Microsoft.AspNetCore.Authorization;

namespace Auth.Api;

public sealed class PermissionInHandler(IPermissionBitCache permissionBitCache) : AuthorizationHandler<PermissionInRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionInRequirement requirement)
    {
        var wsClaim = context.User.FindFirst("ws")?.Value;

        if (string.IsNullOrWhiteSpace(wsClaim))
        {
            return Task.CompletedTask;
        }

        Dictionary<string, Dictionary<string, string>>? workspaceMasks;

        try
        {
            workspaceMasks = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(wsClaim);
        }
        catch
        {
            return Task.CompletedTask;
        }

        if (workspaceMasks is null)
        {
            return Task.CompletedTask;
        }

        if (!workspaceMasks.TryGetValue(requirement.WorkspaceCode, out var domainMasks))
        {
            return Task.CompletedTask;
        }

        if (permissionBitCache.TryGetBit(requirement.Domain, requirement.Permission, out var bit)
            && domainMasks.TryGetValue(requirement.Domain, out var encoded))
        {
            if (PermissionBitmask.HasBit(Convert.FromBase64String(encoded), bit))
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}
