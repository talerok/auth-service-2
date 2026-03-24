using System.Text.Json;
using Auth.Application;
using Microsoft.AspNetCore.Authorization;

namespace Auth.Api;

public sealed class PermissionInHandler(
    IPermissionBitCache permissionBitCache,
    ILogger<PermissionInHandler> logger) : AuthorizationHandler<PermissionInRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionInRequirement requirement)
    {
        var wsClaim = context.User.FindFirst($"ws:{requirement.WorkspaceCode}")?.Value;

        if (string.IsNullOrWhiteSpace(wsClaim))
        {
            return Task.CompletedTask;
        }

        Dictionary<string, string>? domainMasks;

        try
        {
            domainMasks = JsonSerializer.Deserialize<Dictionary<string, string>>(wsClaim);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize workspace claim for ws:{WorkspaceCode}", requirement.WorkspaceCode);
            return Task.CompletedTask;
        }

        if (domainMasks is null)
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
