using System.Security.Claims;
using Auth.Application;
using Auth.Application.Oidc.Commands.GrantConsent;
using Auth.Application.Oidc.Commands.HandleMfaOtpGrant;
using Auth.Application.Oidc.Commands.ValidateCredentialsForLogin;
using Auth.Application.Oidc.Queries.GetClientInfo;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.Api.Controllers;

[ApiController]
[EnableCors("Oidc")]
public sealed class LoginController(ISender sender) : ControllerBase
{
    private const string CookieScheme = "Identity.External";

    // ─── Login (sets cookie for authorize flow) ──────────────────────

    [HttpPost("connect/login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest loginRequest, CancellationToken cancellationToken)
    {
        var returnUrl = SanitizeReturnUrl(loginRequest.ReturnUrl);

        CredentialValidationResult result;
        try
        {
            result = await sender.Send(new ValidateCredentialsForLoginCommand(
                loginRequest.Username, loginRequest.Password, []), cancellationToken);
        }
        catch (AuthException)
        {
            return Unauthorized(new { error = "invalid_credentials" });
        }

        switch (result)
        {
            case CredentialValidationResult.Success s:
                await HttpContext.SignInAsync(CookieScheme, s.Principal);
                return Ok(new { redirect_url = returnUrl ?? "/" });

            case CredentialValidationResult.MfaRequired mfa:
                return BadRequest(new
                {
                    error = "mfa_required",
                    mfa_token = mfa.ChallengeId.ToString(),
                    mfa_channel = mfa.Channel.ToString().ToLowerInvariant(),
                    return_url = returnUrl
                });

            case CredentialValidationResult.PasswordChangeRequired pc:
                return BadRequest(new
                {
                    error = "password_change_required",
                    challenge_id = pc.ChallengeId.ToString(),
                    return_url = returnUrl
                });

            default:
                throw new InvalidOperationException($"Unexpected grant result: {result.GetType().Name}");
        }
    }

    // ─── MFA verification (sets cookie for authorize flow) ──────────

    [HttpPost("connect/mfa/verify")]
    public async Task<IActionResult> MfaVerify(
        [FromBody] MfaVerifyRequest request, CancellationToken cancellationToken)
    {
        var returnUrl = SanitizeReturnUrl(request.ReturnUrl);

        ClaimsPrincipal principal;
        try
        {
            principal = await sender.Send(new HandleMfaOtpGrantCommand(
                request.MfaToken, request.MfaChannel, request.Otp, []), cancellationToken);
        }
        catch (AuthException)
        {
            return Unauthorized(new { error = "invalid_otp" });
        }

        await HttpContext.SignInAsync(CookieScheme, principal);
        return Ok(new { redirect_url = returnUrl ?? "/" });
    }

    // ─── Consent confirmation ────────────────────────────────────────

    [HttpPost("connect/authorize/consent")]
    public async Task<IActionResult> Consent(
        [FromBody] ConsentRequest consentRequest, CancellationToken cancellationToken)
    {
        var authResult = await HttpContext.AuthenticateAsync(CookieScheme);
        if (!authResult.Succeeded || authResult.Principal is null)
            return Unauthorized(new { error = "not_authenticated" });

        var subject = authResult.Principal.FindFirst(Claims.Subject)?.Value;
        if (!Guid.TryParse(subject, out var userId))
            return Unauthorized(new { error = "invalid_subject" });

        var safeReturnUrl = SanitizeReturnUrl(consentRequest.ReturnUrl);

        if (!consentRequest.Approved)
        {
            var separator = safeReturnUrl?.Contains('?') == true ? "&" : "?";
            var denyUrl = safeReturnUrl != null
                ? $"{safeReturnUrl}{separator}consent_denied=true"
                : null;

            return Ok(new { redirect_url = denyUrl });
        }

        await sender.Send(new GrantConsentCommand(
            consentRequest.ClientId, userId, consentRequest.Scopes.ToList()), cancellationToken);

        return Ok(new { redirect_url = safeReturnUrl });
    }

    // ─── Client info (for consent page) ─────────────────────────────

    [HttpGet("connect/client-info")]
    public async Task<IActionResult> ClientInfo(
        [FromQuery(Name = "client_id")] string clientId, CancellationToken cancellationToken)
    {
        var info = await sender.Send(new GetClientInfoQuery(clientId), cancellationToken);
        if (info is null)
            return NotFound();

        return Ok(info);
    }

    // ─── Utilities ───────────────────────────────────────────────────

    private static string? SanitizeReturnUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        if (!url.StartsWith('/'))
            return null;

        if (url.StartsWith("//", StringComparison.Ordinal))
            return null;

        return url;
    }
}
