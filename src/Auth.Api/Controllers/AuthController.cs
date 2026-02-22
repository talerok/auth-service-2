using Auth.Application;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService, ITwoFactorAuthService twoFactorAuthService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await authService.LoginAsync(request, cancellationToken));

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokensResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthTokensResponse>> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken) =>
        Ok(await authService.RefreshAsync(request, cancellationToken));

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken) =>
        Ok(await authService.RegisterAsync(request, cancellationToken));

    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> Revoke([FromBody] RevokeRequest request, CancellationToken cancellationToken)
    {
        await authService.RevokeAsync(request, cancellationToken);
        return NoContent();
    }

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

    [HttpPost("2fa/login/verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokensResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthTokensResponse>> VerifyTwoFactorLogin(
        [FromBody] VerifyTwoFactorRequest request,
        CancellationToken cancellationToken) =>
        Ok(await twoFactorAuthService.VerifyTwoFactorLoginAsync(request, cancellationToken));

    [HttpPost("password/forced-change")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokensResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthTokensResponse>> ForcedChangePassword(
        [FromBody] ForcedPasswordChangeRequest request,
        CancellationToken cancellationToken) =>
        Ok(await authService.ForcedChangePasswordAsync(request, cancellationToken));

    private Guid GetUserId()
    {
        var subject = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(subject, out var userId))
        {
            throw new AuthException(AuthErrorCatalog.InvalidUserContext);
        }

        return userId;
    }
}
