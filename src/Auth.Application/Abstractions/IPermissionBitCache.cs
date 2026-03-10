namespace Auth.Application;

public interface IPermissionBitCache
{
    bool TryGetBit(string domain, string code, out int bit);
    IReadOnlyDictionary<(string Domain, string Code), int> Snapshot();
    Task WarmupAsync(CancellationToken cancellationToken);
}
