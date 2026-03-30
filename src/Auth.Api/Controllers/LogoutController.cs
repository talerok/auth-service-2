using Auth.Application.Sessions.Commands.RevokeOwnSession;
using MediatR;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;

namespace Auth.Api.Controllers;

[ApiController]
[EnableCors("Oidc")]
public sealed class LogoutController(ISender sender, ILogger<LogoutController> logger) : ControllerBase
{
    private const string CookieScheme = "Identity.External";

    [HttpGet("connect/logout")]
    [HttpPost("connect/logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var sidClaim = User.FindFirst("sid")?.Value;
        var subClaim = User.FindFirst("sub")?.Value;
        if (Guid.TryParse(sidClaim, out var sessionId) && Guid.TryParse(subClaim, out var userId))
            await sender.Send(new RevokeOwnSessionCommand(sessionId, userId), cancellationToken);
        else
            logger.LogWarning("Logout without session revocation: sid={Sid}, sub={Sub}", sidClaim, subClaim);

        await HttpContext.SignOutAsync(CookieScheme);

        var request = HttpContext.GetOpenIddictServerRequest();
        var redirectUri = request?.PostLogoutRedirectUri ?? "/";

        return SignOut(
            authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
            properties: new AuthenticationProperties { RedirectUri = redirectUri });
    }
}
