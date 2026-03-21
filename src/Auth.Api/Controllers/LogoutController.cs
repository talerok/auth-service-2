using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;

namespace Auth.Api.Controllers;

[ApiController]
public sealed class LogoutController : ControllerBase
{
    private const string CookieScheme = "Identity.External";

    [HttpGet("connect/logout")]
    [HttpPost("connect/logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieScheme);

        return SignOut(
            authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
            properties: new AuthenticationProperties { RedirectUri = "/" });
    }
}
