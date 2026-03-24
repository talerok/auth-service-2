using Auth.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Auth.UnitTests;

internal static class TestDbContextFactory
{
    public static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AuthDbContext(options);
    }
}
