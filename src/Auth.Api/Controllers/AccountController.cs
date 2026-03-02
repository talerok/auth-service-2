using Auth.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/account")]
public sealed class AccountController(IAuthService authService, ITwoFactorAuthService twoFactorAuthService) : ControllerBase
{
    [HttpPost("2fa/enable")]
    [Authorize]
    [ProducesResponseType(typeof(EnableTwoFactorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EnableTwoFactorResponse>> EnableTwoFactor(
        [FromBody] EnableTwoFactorRequest request,
        CancellationToken cancellationToken) =>
        Ok(await twoFactorAuthService.EnableTwoFactorAsync(GetUserId(), request, cancellationToken));

    [HttpPost("2fa/confirm")]
    [Authorize]
    public async Task<IActionResult> ConfirmTwoFactor(
        [FromBody] VerifyTwoFactorRequest request,
        CancellationToken cancellationToken)
    {
        await twoFactorAuthService.ConfirmTwoFactorActivationAsync(GetUserId(), request, cancellationToken);
        return NoContent();
    }

    [HttpPost("2fa/disable")]
    [Authorize]
    public async Task<IActionResult> DisableTwoFactor(CancellationToken cancellationToken)
    {
        await twoFactorAuthService.DisableTwoFactorAsync(GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("password/forced-change")]
    [AllowAnonymous]
    public async Task<IActionResult> ForcedPasswordChange(
        [FromBody] ForcedPasswordChangeRequest request,
        CancellationToken cancellationToken)
    {
        await authService.ValidateForcedPasswordChangeAsync(request, cancellationToken);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var subject = User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;
        if (!Guid.TryParse(subject, out var userId))
        {
            throw new AuthException(AuthErrorCatalog.InvalidUserContext);
        }

        return userId;
    }
}
