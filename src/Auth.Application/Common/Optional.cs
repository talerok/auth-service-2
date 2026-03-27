namespace Auth.Application.Common;

public interface IOptional
{
    bool HasValue { get; }
}

/// <summary>
/// Wraps a value to distinguish between "not provided" (undefined) and "explicitly set" (including null).
/// Used in PATCH operations: if a JSON field is absent, HasValue is false; if present (even as null), HasValue is true.
/// </summary>
public readonly struct Optional<T> : IOptional
{
    private readonly T? _value;
    private readonly bool _hasValue;

    public Optional(T? value)
    {
        _value = value;
        _hasValue = true;
    }

    public bool HasValue => _hasValue;

    public T? Value => _hasValue
        ? _value
        : throw new InvalidOperationException("Optional value was not provided.");

    public static implicit operator Optional<T>(T? value) => new(value);
}
