using Auth.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Infrastructure.Cors;

internal sealed class CorsOriginService(
    IServiceScopeFactory scopeFactory,
    IMemoryCache memoryCache) : ICorsOriginService
{
    private const string CacheKey = "cors:allowed-origins";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public bool IsOriginAllowed(string origin)
    {
        var origins = GetAllowedOrigins();
        return origins.Contains(origin);
    }

    public void InvalidateCache()
    {
        memoryCache.Remove(CacheKey);
    }

    private HashSet<string> GetAllowedOrigins() =>
        memoryCache.GetOrCreate(CacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;

            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            var dbOrigins = dbContext.Applications
                .AsNoTracking()
                .Where(a => a.IsActive)
                .SelectMany(a => a.AllowedOrigins)
                .Distinct()
                .ToList();

            return new HashSet<string>(dbOrigins, StringComparer.OrdinalIgnoreCase);
        })!;
}
