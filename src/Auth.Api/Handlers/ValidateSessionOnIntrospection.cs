using Auth.Infrastructure;
using Microsoft.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenIddict.Server;

namespace Auth.Api.Handlers;

public sealed class ValidateSessionOnIntrospection
    : IOpenIddictServerHandler<OpenIddictServerEvents.HandleIntrospectionRequestContext>
{
    public async ValueTask HandleAsync(OpenIddictServerEvents.HandleIntrospectionRequestContext context)
    {
        if (context is null || context.GenericTokenPrincipal is null)
            return;

        var httpRequest = context.Transaction.GetHttpRequest()
            ?? throw new InvalidOperationException("The HTTP request cannot be retrieved.");
        var ct = httpRequest.HttpContext.RequestAborted;
        var services = httpRequest.HttpContext.RequestServices;
        var logger = services.GetRequiredService<ILogger<ValidateSessionOnIntrospection>>();

        var sidClaim = context.GenericTokenPrincipal.FindFirst("sid")?.Value;
        if (Guid.TryParse(sidClaim, out var sessionId))
        {
            var dbContext = services.GetRequiredService<AuthDbContext>();
            var isActive = await dbContext.UserSessions
                .Where(s => s.Id == sessionId)
                .Join(dbContext.Users, s => s.UserId, u => u.Id, (s, u) => new { s, u })
                .Select(x => (bool?)(!x.s.IsRevoked && x.s.ExpiresAt > DateTime.UtcNow && x.u.IsActive))
                .FirstOrDefaultAsync(ct);

            if (isActive != true)
            {
                var subClaim = context.GenericTokenPrincipal.FindFirst("sub")?.Value;
                logger.LogInformation("Introspection rejected: session inactive. sub={Sub}, sid={Sid}", subClaim, sidClaim);
                context.GenericTokenPrincipal = null!; // signals OpenIddict that the token is inactive
            }
        }
    }
}
