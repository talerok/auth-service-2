namespace Auth.Api;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
        context.Response.Headers["X-Correlation-Id"] = correlationId;
        context.Items["CorrelationId"] = correlationId;
        await next(context);
    }
}
