using Auth.Application;
using Auth.Domain;
using FluentValidation;

namespace Auth.Api;

public sealed class ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException ex)
        {
            var problem = new AuthProblemDescriptor(
                StatusCodes.Status400BadRequest,
                "Business rule violation",
                "System entities cannot be modified");
            await ProblemDetailsResponseWriter.WriteAsync(context, problem, ex.Code);
        }
        catch (AuthException ex)
        {
            var problem = AuthProblemDetailsMapper.Map(ex);
            await ProblemDetailsResponseWriter.WriteAsync(context, problem, ex.Code);
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            var problem = new AuthProblemDescriptor(
                StatusCodes.Status400BadRequest,
                "Validation Error",
                "One or more validation errors occurred");

            await ProblemDetailsResponseWriter.WriteValidationAsync(context, problem, errors);
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
