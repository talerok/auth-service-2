using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;

namespace Auth.Api.Controllers;

[ApiController]
[EnableCors("Oidc")]
public sealed class LogoutController : ControllerBase
{
    private const string CookieScheme = "Identity.External";

    [HttpGet("connect/logout")]
    [HttpPost("connect/logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieScheme);

        var request = HttpContext.GetOpenIddictServerRequest();
        var redirectUri = request?.PostLogoutRedirectUri ?? "/";

        return SignOut(
            authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
            properties: new AuthenticationProperties { RedirectUri = redirectUri });
    }
}
