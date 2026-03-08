using Auth.Application;
using Auth.Application.Auth.Commands.ValidateForcedPasswordChange;
using Auth.Application.TwoFactor.Commands.ConfirmTwoFactorActivation;
using Auth.Application.TwoFactor.Commands.DisableTwoFactor;
using Auth.Application.TwoFactor.Commands.EnableTwoFactor;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/account")]
public sealed class AccountController(ISender sender, IOptions<PasswordRequirementsOptions> passwordOptions) : ControllerBase
{
    [HttpGet("password-requirements")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PasswordRequirementsOptions), StatusCodes.Status200OK)]
    public ActionResult<PasswordRequirementsOptions> GetPasswordRequirements() =>
        Ok(passwordOptions.Value);

    [HttpPost("2fa/enable")]
    [Authorize]
    [ProducesResponseType(typeof(EnableTwoFactorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EnableTwoFactorResponse>> EnableTwoFactor(
        [FromBody] EnableTwoFactorRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new EnableTwoFactorCommand(GetUserId(), request.Channel, request.IsHighRisk), cancellationToken));

    [HttpPost("2fa/confirm")]
    [Authorize]
    public async Task<IActionResult> ConfirmTwoFactor(
        [FromBody] VerifyTwoFactorRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ConfirmTwoFactorActivationCommand(GetUserId(), request.ChallengeId, request.Channel, request.Otp), cancellationToken);
        return NoContent();
    }

    [HttpPost("2fa/disable")]
    [Authorize]
    public async Task<IActionResult> DisableTwoFactor(CancellationToken cancellationToken)
    {
        await sender.Send(new DisableTwoFactorCommand(GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpPost("password/forced-change")]
    [AllowAnonymous]
    public async Task<IActionResult> ForcedPasswordChange(
        [FromBody] ForcedPasswordChangeRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ValidateForcedPasswordChangeCommand(request.ChallengeId, request.NewPassword), cancellationToken);
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
