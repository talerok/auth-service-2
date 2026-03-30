using Microsoft.AspNetCore.Http;

namespace Auth.Infrastructure;

internal static class HttpContextAccessorExtensions
{
    public static (string IpAddress, string UserAgent) GetClientInfo(this IHttpContextAccessor accessor)
    {
        var ctx = accessor.HttpContext;
        return (
            ctx?.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            ctx?.Request.Headers.UserAgent.FirstOrDefault() ?? "unknown");
    }
}
