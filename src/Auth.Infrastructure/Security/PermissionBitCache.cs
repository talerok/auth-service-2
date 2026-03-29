using Auth.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Auth.Infrastructure;

internal sealed class PermissionBitCache(
    IServiceScopeFactory scopeFactory,
    IDistributedCache distributedCache,
    ILogger<PermissionBitCache> logger) : IPermissionBitCache
{
    private const string CacheKey = "permissions:bit-cache";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private volatile Dictionary<string, int> _local = [];

    public bool TryGetBit(string domain, string code, out int bit)
    {
        var key = $"{domain.ToLowerInvariant()}:{code.ToLowerInvariant()}";
        return _local.TryGetValue(key, out bit);
    }

    public IReadOnlyDictionary<(string Domain, string Code), int> Snapshot()
    {
        var result = new Dictionary<(string, string), int>();
        foreach (var (key, value) in _local)
        {
            var parts = key.Split(':', 2);
            if (parts.Length == 2)
                result[(parts[0], parts[1])] = value;
        }
        return result;
    }

    public async Task WarmupAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var permissions = await dbContext.Permissions.AsNoTracking().ToListAsync(cancellationToken);

        var dict = new Dictionary<string, int>();
        foreach (var permission in permissions)
        {
            dict[$"{permission.Domain.ToLowerInvariant()}:{permission.Code.ToLowerInvariant()}"] = permission.Bit;
        }

        _local = dict;

        try
        {
            var json = JsonSerializer.Serialize(dict);
            await distributedCache.SetStringAsync(CacheKey, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write permission bit cache to Redis");
        }
    }
}
