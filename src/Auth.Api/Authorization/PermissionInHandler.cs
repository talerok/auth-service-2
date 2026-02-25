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
            return Task.CompletedTask;

        Dictionary<string, string>? workspaceMasks;
        try
        {
            workspaceMasks = JsonSerializer.Deserialize<Dictionary<string, string>>(wsClaim);
        }
        catch
        {
            return Task.CompletedTask;
        }

        if (workspaceMasks is null || !workspaceMasks.TryGetValue(requirement.WorkspaceCode, out var encoded))
            return Task.CompletedTask;

        var bytes = Convert.FromBase64String(encoded);

        foreach (var bit in GetMatchingBits(requirement.PermissionPattern))
        {
            if (PermissionBitmask.HasBit(bytes, bit))
            {
                context.Succeed(requirement);
                break;
            }
        }

        return Task.CompletedTask;
    }

    private IEnumerable<int> GetMatchingBits(string pattern)
    {
        if (!pattern.EndsWith(".*", StringComparison.Ordinal))
        {
            if (permissionBitCache.TryGetBitByCode(pattern, out var bit))
                yield return bit;
            yield break;
        }

        var prefix = pattern[..^2];
        foreach (var (code, bit) in permissionBitCache.Snapshot())
        {
            if (code == prefix || code.StartsWith(prefix + ".", StringComparison.Ordinal))
                yield return bit;
        }
    }
}
