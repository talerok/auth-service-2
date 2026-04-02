namespace Auth.Api.RateLimit;

public static class RateLimitPolicies
{
    public const string Auth = "auth";
    public const string TwoFactor = "twofactor";
    public const string Global = "global";
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RateLimitAttribute(string policy) : Attribute
{
    public string Policy { get; } = policy;
}
