using Auth.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Auth.Infrastructure.Cors;

internal sealed class CorsOriginService(
    IServiceScopeFactory scopeFactory,
    IDistributedCache distributedCache,
    ILogger<CorsOriginService> logger) : ICorsOriginService
{
    private const string CacheKey = "cors:allowed-origins";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private volatile HashSet<string>? _local;

    public bool IsOriginAllowed(string origin)
    {
        var origins = GetAllowedOrigins();
        return origins.Contains(origin);
    }

    public void InvalidateCache()
    {
        _local = LoadFromDatabase();
    }

    private HashSet<string> GetAllowedOrigins()
    {
        var local = _local;
        if (local is not null)
            return local;

        try
        {
            var cached = distributedCache.GetString(CacheKey);
            if (cached is not null)
            {
                var origins = JsonSerializer.Deserialize<List<string>>(cached) ?? [];
                var set = new HashSet<string>(origins, StringComparer.OrdinalIgnoreCase);
                _local = set;
                return set;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read CORS origin cache from Redis");
        }

        return LoadFromDatabase();
    }

    private HashSet<string> LoadFromDatabase()
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var dbOrigins = dbContext.Applications
            .AsNoTracking()
            .Where(a => a.IsActive)
            .Select(a => a.AllowedOrigins)
            .ToList()
            .SelectMany(o => o)
            .Distinct()
            .ToList();

        var set = new HashSet<string>(dbOrigins, StringComparer.OrdinalIgnoreCase);
        _local = set;

        try
        {
            distributedCache.SetString(CacheKey,
                JsonSerializer.Serialize(dbOrigins),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write CORS origin cache to Redis");
        }

        return set;
    }
}
