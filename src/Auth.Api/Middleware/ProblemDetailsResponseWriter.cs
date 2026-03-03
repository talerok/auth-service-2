using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Auth.Api;

public static class ProblemDetailsResponseWriter
{
    public static Task WriteAsync(HttpContext context, AuthProblemDescriptor descriptor, string? code = null)
    {
        context.Response.StatusCode = descriptor.StatusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = descriptor.StatusCode,
            Title = descriptor.Title,
            Detail = descriptor.Detail,
            Type = $"https://httpstatuses.com/{descriptor.StatusCode}",
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;
        if (!string.IsNullOrWhiteSpace(code))
        {
            problem.Extensions["code"] = code;
        }

        return JsonSerializer.SerializeAsync(context.Response.Body, problem, cancellationToken: context.RequestAborted);
    }

    public static Task WriteValidationAsync(
        HttpContext context,
        AuthProblemDescriptor descriptor,
        IDictionary<string, string[]> errors)
    {
        context.Response.StatusCode = descriptor.StatusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = descriptor.StatusCode,
            Title = descriptor.Title,
            Detail = descriptor.Detail,
            Type = $"https://httpstatuses.com/{descriptor.StatusCode}",
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;
        problem.Extensions["errors"] = errors;

        return JsonSerializer.SerializeAsync(context.Response.Body, problem, cancellationToken: context.RequestAborted);
    }
}
