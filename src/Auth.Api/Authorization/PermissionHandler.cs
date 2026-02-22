using System.Text.Json;
using Auth.Application;
using Microsoft.AspNetCore.Authorization;

namespace Auth.Api;

public sealed class PermissionHandler(IPermissionBitCache permissionBitCache) : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (!permissionBitCache.TryGetBitByCode(requirement.PermissionCode, out var bit))
        {
            return Task.CompletedTask;
        }

        var wsClaim = context.User.FindFirst("ws")?.Value;
        if (string.IsNullOrWhiteSpace(wsClaim))
        {
            return Task.CompletedTask;
        }

        Dictionary<string, string>? workspaceMasks;
        try
        {
            workspaceMasks = JsonSerializer.Deserialize<Dictionary<string, string>>(wsClaim);
        }
        catch
        {
            return Task.CompletedTask;
        }

        if (workspaceMasks is null || workspaceMasks.Count == 0)
        {
            return Task.CompletedTask;
        }

        if (requirement.InWorkspace)
        {
            if (context.Resource is not HttpContext httpContext)
            {
                return Task.CompletedTask;
            }

            var workspaceId = httpContext.Request.RouteValues["workspaceId"]?.ToString();
            if (workspaceId is null || !workspaceMasks.TryGetValue(workspaceId, out var encoded))
            {
                return Task.CompletedTask;
            }

            if (HasBit(encoded, bit))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }

        foreach (var encoded in workspaceMasks.Values)
        {
            if (HasBit(encoded, bit))
            {
                context.Succeed(requirement);
                break;
            }
        }

        return Task.CompletedTask;
    }

    private static bool HasBit(string encodedMask, int bit)
    {
        var bytes = Convert.FromBase64String(encodedMask);
        return PermissionBitmask.HasBit(bytes, bit);
    }
}
