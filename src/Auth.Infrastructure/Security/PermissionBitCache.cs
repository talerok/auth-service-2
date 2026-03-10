using Auth.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Infrastructure;

public sealed class PermissionBitCache(IServiceScopeFactory scopeFactory) : IPermissionBitCache
{
    private readonly Dictionary<(string, string), int> _cache = [];

    public bool TryGetBit(string domain, string code, out int bit) =>
        _cache.TryGetValue((domain.ToLowerInvariant(), code.ToLowerInvariant()), out bit);

    public IReadOnlyDictionary<(string Domain, string Code), int> Snapshot() => _cache;

    public async Task WarmupAsync(CancellationToken cancellationToken)
    {
        _cache.Clear();
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var permissions = await dbContext.Permissions.AsNoTracking().ToListAsync(cancellationToken);
        foreach (var permission in permissions)
        {
            _cache[(permission.Domain.ToLowerInvariant(), permission.Code.ToLowerInvariant())] = permission.Bit;
        }
    }
}
