namespace Auth.Application;

public interface IDistributedLock
{
    /// <summary>Waits until the lock is acquired. Automatically released on Dispose.</summary>
    Task<IAsyncDisposable> AcquireAsync(string resource, CancellationToken ct = default);

    /// <summary>Attempts to acquire the lock without waiting. Returns null if already held by another instance.</summary>
    Task<IAsyncDisposable?> TryAcquireAsync(string resource, CancellationToken ct = default);
}
