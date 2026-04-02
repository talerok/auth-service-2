using Auth.Infrastructure;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Auth.Api.RateLimit;

public sealed class RateLimitMiddleware(RequestDelegate next, IConnectionMultiplexer redis, IOptions<RateLimitOptions> options, ILogger<RateLimitMiddleware> logger)
{
    private static readonly LuaScript IncrementScript = LuaScript.Prepare(
        """
        local count = redis.call('INCR', @key)
        if count == 1 then
            redis.call('EXPIRE', @key, @ttl)
        end
        return count
        """);

    private readonly RateLimitOptions _options = options.Value;

    public async Task Invoke(HttpContext context)
    {
        var (policyName, policy) = ResolvePolicy(context);
        if (policy.PermitLimit <= 0 || policy.WindowSeconds <= 0)
        {
            await next(context);
            return;
        }

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var windowStart = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / policy.WindowSeconds;
        var redisKey = $"auth:ratelimit:{policyName}:{ip}:{windowStart}";

        long count;
        try
        {
            var db = redis.GetDatabase();
            count = (long)await db.ScriptEvaluateAsync(IncrementScript, new { key = (RedisKey)redisKey, ttl = policy.WindowSeconds });
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Rate limit Redis check failed, allowing request");
            await next(context);
            return;
        }

        if (count > policy.PermitLimit)
        {
            var retryAfter = policy.WindowSeconds - (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % policy.WindowSeconds);
            context.Response.Headers.RetryAfter = retryAfter.ToString();

            logger.LogWarning("Rate limit exceeded for {Policy} from {Ip}: {Count}/{Limit}", policyName, ip, count, policy.PermitLimit);

            await ProblemDetailsResponseWriter.WriteAsync(context,
                new AuthProblemDescriptor(StatusCodes.Status429TooManyRequests, "Too Many Requests", "Rate limit exceeded. Try again later."));
            return;
        }

        await next(context);
    }

    private (string Name, RateLimitPolicyOptions Policy) ResolvePolicy(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var policyName = endpoint?.Metadata.GetMetadata<RateLimitAttribute>()?.Policy ?? RateLimitPolicies.Global;

        var policy = policyName switch
        {
            RateLimitPolicies.Auth => _options.Auth,
            RateLimitPolicies.TwoFactor => _options.TwoFactor,
            _ => _options.Global
        };

        return (policyName, policy);
    }
}
