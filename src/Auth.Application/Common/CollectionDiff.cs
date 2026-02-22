namespace Auth.Application;

public sealed record CollectionDiffResult<TId>(IReadOnlyCollection<TId> ToAdd, IReadOnlyCollection<TId> ToRemove);

public static class CollectionDiff
{
    public static CollectionDiffResult<TId> Calculate<TId>(
        IReadOnlyCollection<TId> desiredIds,
        IReadOnlyCollection<TId> currentIds)
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(desiredIds);
        ArgumentNullException.ThrowIfNull(currentIds);

        var normalizedDesired = desiredIds.Distinct().ToArray();
        if (normalizedDesired.Length != desiredIds.Count)
        {
            throw new AuthException(AuthErrorCatalog.DuplicateIdsNotAllowed);
        }

        var normalizedCurrent = currentIds.Distinct().ToArray();
        var toAdd = normalizedDesired.Except(normalizedCurrent).ToArray();
        var toRemove = normalizedCurrent.Except(normalizedDesired).ToArray();

        return new CollectionDiffResult<TId>(toAdd, toRemove);
    }
}
