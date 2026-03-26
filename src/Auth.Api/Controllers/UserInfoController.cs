using Auth.Application.Oidc.Queries.GetUserInfo;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.Api.Controllers;

[ApiController]
[EnableCors("Oidc")]
public sealed class UserInfoController(ISender sender) : ControllerBase
{
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("connect/userinfo")]
    [HttpPost("connect/userinfo")]
    public async Task<IActionResult> UserInfo(CancellationToken cancellationToken)
    {
        var subject = User.FindFirst(Claims.Subject)?.Value;
        if (!Guid.TryParse(subject, out var userId))
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        var scopes = User.GetScopes().ToList();
        var userInfo = new GetUserInfoQuery(userId, scopes);
        var claims = await sender.Send(userInfo, cancellationToken);
        return Ok(claims);
    }
}
