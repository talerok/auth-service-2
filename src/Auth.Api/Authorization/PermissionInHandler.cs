using System.Text.Json;
using Auth.Application;
using Microsoft.AspNetCore.Authorization;

namespace Auth.Api;

public sealed class PermissionInHandler(IPermissionBitCache permissionBitCache) : AuthorizationHandler<PermissionInRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionInRequirement requirement)
    {
        var wsClaim = context.User.FindFirst("ws")?.Value;
        Dictionary<string, string>? workspaceMasks;

        if (string.IsNullOrWhiteSpace(wsClaim))
        {
            return Task.CompletedTask;
        }


        try
        {
            workspaceMasks = JsonSerializer.Deserialize<Dictionary<string, string>>(wsClaim);
        }
        catch
        {
            return Task.CompletedTask;
        }

        if (workspaceMasks is null)
        {
            return Task.CompletedTask;
        }

        if (!workspaceMasks.TryGetValue(requirement.WorkspaceCode, out var encoded))
        {
            return Task.CompletedTask;
        }

        var hasBit = permissionBitCache.TryGetBitByCode(requirement.Permission, out var bit);
        if (hasBit && PermissionBitmask.HasBit(Convert.FromBase64String(encoded), bit))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
