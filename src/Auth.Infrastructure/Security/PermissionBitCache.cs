using Auth.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Infrastructure;

public sealed class PermissionBitCache(IServiceScopeFactory scopeFactory) : IPermissionBitCache
{
    private readonly Dictionary<string, int> _cache = new(StringComparer.OrdinalIgnoreCase);

    public int GetBitByCode(string code) => _cache[code];
    public bool TryGetBitByCode(string code, out int bit) => _cache.TryGetValue(code, out bit);
    public IReadOnlyDictionary<string, int> Snapshot() => _cache;

    public async Task WarmupAsync(CancellationToken cancellationToken)
    {
        _cache.Clear();
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var permissions = await dbContext.Permissions.AsNoTracking().ToListAsync(cancellationToken);
        foreach (var permission in permissions)
        {
            _cache[permission.Code] = permission.Bit;
        }
    }
}
