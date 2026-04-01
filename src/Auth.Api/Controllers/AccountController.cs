using Auth.Application;
using Auth.Application.Auth.Commands.ValidateForcedPasswordChange;
using Auth.Application.TwoFactor.Commands.ConfirmTwoFactorActivation;

using Auth.Application.TwoFactor.Commands.EnableTwoFactor;
using Auth.Application.Verification;
using Auth.Application.Verification.Commands.ConfirmEmailVerification;
using Auth.Application.Verification.Commands.ConfirmPhoneVerification;
using Auth.Application.Verification.Commands.SendEmailVerification;
using Auth.Application.Sessions;
using Auth.Application.Sessions.Commands.RevokeOwnSession;
using Auth.Application.Sessions.Commands.RevokeUserSessions;
using Auth.Application.Sessions.Queries.GetUserSessions;
using Auth.Application.Verification.Commands.SendPhoneVerification;
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

    [HttpPost("password/forced-change")]
    [AllowAnonymous]
    public async Task<IActionResult> ForcedPasswordChange(
        [FromBody] ForcedPasswordChangeRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ValidateForcedPasswordChangeCommand(request.ChallengeId, request.NewPassword), cancellationToken);
        return NoContent();
    }

    [HttpPost("verify-email/send")]
    [Authorize]
    public async Task<ActionResult<SendVerificationResponse>> SendEmailVerification(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new SendEmailVerificationCommand(GetUserId()), cancellationToken));

    [HttpPost("verify-email/confirm")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmailVerification(
        [FromBody] ConfirmVerificationRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ConfirmEmailVerificationCommand(request.ChallengeId, request.Otp), cancellationToken);
        return NoContent();
    }

    [HttpPost("verify-phone/send")]
    [Authorize]
    public async Task<ActionResult<SendVerificationResponse>> SendPhoneVerification(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new SendPhoneVerificationCommand(GetUserId()), cancellationToken));

    [HttpPost("verify-phone/confirm")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmPhoneVerification(
        [FromBody] ConfirmVerificationRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ConfirmPhoneVerificationCommand(request.ChallengeId, request.Otp), cancellationToken);
        return NoContent();
    }

    [HttpGet("sessions")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyCollection<UserSessionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyCollection<UserSessionResponse>>> GetSessions(CancellationToken ct)
    {
        var currentSid = User.FindFirst("sid")?.Value;
        Guid? currentSessionId = Guid.TryParse(currentSid, out var parsed) ? parsed : null;
        return Ok(await sender.Send(new GetUserSessionsQuery(GetUserId(), currentSessionId), ct));
    }

    [HttpDelete("sessions/{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeSession(Guid id, CancellationToken ct)
    {
        await sender.Send(new RevokeOwnSessionCommand(id, GetUserId()), ct);
        return NoContent();
    }

    [HttpDelete("sessions")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeAllSessions(CancellationToken ct)
    {
        await sender.Send(new RevokeUserSessionsCommand(GetUserId(), "logout-all"), ct);
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
