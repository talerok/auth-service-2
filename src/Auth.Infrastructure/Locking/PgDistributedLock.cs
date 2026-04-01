using System.Security.Cryptography;
using System.Text;
using Auth.Application;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Auth.Infrastructure.Locking;

internal sealed class PgDistributedLock(IOptions<IntegrationOptions> options) : IDistributedLock
{
    public async Task<IAsyncDisposable> AcquireAsync(string resource, CancellationToken ct = default)
    {
        var key = HashToLong(resource);
        var conn = await OpenConnectionAsync(ct);
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT pg_advisory_lock(@key)";
            cmd.Parameters.AddWithValue("key", key);
            await cmd.ExecuteNonQueryAsync(ct);
            return new LockHandle(conn, key);
        }
        catch
        {
            await conn.DisposeAsync();
            throw;
        }
    }

    public async Task<IAsyncDisposable?> TryAcquireAsync(string resource, CancellationToken ct = default)
    {
        var key = HashToLong(resource);
        var conn = await OpenConnectionAsync(ct);
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT pg_try_advisory_lock(@key)";
            cmd.Parameters.AddWithValue("key", key);
            var acquired = (bool)(await cmd.ExecuteScalarAsync(ct))!;
            if (!acquired)
            {
                await conn.DisposeAsync();
                return null;
            }
            return new LockHandle(conn, key);
        }
        catch
        {
            await conn.DisposeAsync();
            throw;
        }
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken ct)
    {
        var conn = new NpgsqlConnection(options.Value.PostgreSql.ConnectionString);
        await conn.OpenAsync(ct);
        return conn;
    }

    private static long HashToLong(string resource)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(resource));
        return BitConverter.ToInt64(hash, 0);
    }

    private sealed class LockHandle(NpgsqlConnection conn, long key) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT pg_advisory_unlock(@key)";
                cmd.Parameters.AddWithValue("key", key);
                await cmd.ExecuteNonQueryAsync();
            }
            finally
            {
                await conn.DisposeAsync();
            }
        }
    }
}
