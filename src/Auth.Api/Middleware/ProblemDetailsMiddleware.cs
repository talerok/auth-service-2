using Auth.Application;
namespace Auth.Api;

public sealed class ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AuthException ex)
        {
            var problem = AuthProblemDetailsMapper.Map(ex);
            await ProblemDetailsResponseWriter.WriteAsync(context, problem, ex.Code);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            var problem = new AuthProblemDescriptor(
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "Unexpected error");
            await ProblemDetailsResponseWriter.WriteAsync(context, problem);
        }
    }
}
