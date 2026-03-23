using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.Api.Controllers;

[ApiController]
[EnableCors("Oidc")]
public sealed class UserInfoController : ControllerBase
{
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("connect/userinfo")]
    [HttpPost("connect/userinfo")]
    public IActionResult UserInfo()
    {
        var claims = new Dictionary<string, object?>();

        var sub = User.FindFirst(Claims.Subject)?.Value;
        if (sub is not null) claims[Claims.Subject] = sub;

        var name = User.FindFirst(Claims.Name)?.Value;
        if (name is not null) claims[Claims.Name] = name;

        var preferredUsername = User.FindFirst(Claims.PreferredUsername)?.Value;
        if (preferredUsername is not null) claims[Claims.PreferredUsername] = preferredUsername;

        var email = User.FindFirst(Claims.Email)?.Value;
        if (email is not null) claims[Claims.Email] = email;

        var phone = User.FindFirst(Claims.PhoneNumber)?.Value;
        if (phone is not null) claims[Claims.PhoneNumber] = phone;

        return Ok(claims);
    }
}
