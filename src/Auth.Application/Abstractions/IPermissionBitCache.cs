namespace Auth.Application;

public interface IPermissionBitCache
{
    int GetBitByCode(string code);
    bool TryGetBitByCode(string code, out int bit);
    IReadOnlyDictionary<string, int> Snapshot();
    Task WarmupAsync(CancellationToken cancellationToken);
}
