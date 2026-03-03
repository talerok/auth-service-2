using System.Security.Claims;
using Auth.Application;
using Auth.Domain;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.Api.Controllers;

[ApiController]
public sealed class AuthorizationController(
    IOidcGrantService oidcGrantService,
    IIdentitySourceAuthService identitySourceAuthService) : ControllerBase
{
    [HttpPost("connect/token")]
    public async Task<IActionResult> Exchange(CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsPasswordGrantType())
            return await HandlePasswordGrant(request, cancellationToken);

        if (request.IsRefreshTokenGrantType())
            return await HandleRefreshGrant(cancellationToken);

        if (request.GrantType == OidcConstants.MfaOtpGrantType)
            return await HandleMfaOtpGrant(request, cancellationToken);

        if (request.GrantType == OidcConstants.TokenExchangeGrantType)
            return await HandleTokenExchange(request, cancellationToken);

        if (request.IsClientCredentialsGrantType())
            return await HandleClientCredentialsGrant(request, cancellationToken);

        return OidcForbid(Errors.UnsupportedGrantType, "The specified grant type is not supported.");
    }

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

    private async Task<IActionResult> HandlePasswordGrant(
        OpenIddictRequest request, CancellationToken cancellationToken)
    {
        PasswordGrantResult result;
        try
        {
            result = await oidcGrantService.HandlePasswordGrantAsync(
                request.Username!, request.Password!, request.GetScopes().ToList(), cancellationToken);
        }
        catch (AuthException)
        {
            return OidcForbid(Errors.InvalidGrant, "The username/password couple is invalid.");
        }

        return result switch
        {
            PasswordGrantResult.Success s =>
                SignIn(s.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme),

            PasswordGrantResult.MfaRequired mfa =>
                BadRequest(new
                {
                    error = "mfa_required",
                    error_description = "Multi-factor authentication is required.",
                    mfa_token = mfa.ChallengeId.ToString(),
                    mfa_channel = mfa.Channel.ToString().ToLowerInvariant()
                }),

            PasswordGrantResult.PasswordChangeRequired pc =>
                BadRequest(new
                {
                    error = "password_change_required",
                    error_description = "The user must change their password.",
                    challenge_id = pc.ChallengeId.ToString()
                }),

            _ => throw new InvalidOperationException($"Unexpected grant result: {result.GetType().Name}")
        };
    }

    private async Task<IActionResult> HandleRefreshGrant(CancellationToken cancellationToken)
    {
        var authResult = await HttpContext.AuthenticateAsync(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        if (!authResult.Succeeded || authResult.Principal is null)
            return OidcForbid(Errors.InvalidGrant, "The token is no longer valid.");

        var subject = authResult.Principal.FindFirst(Claims.Subject)?.Value;
        if (!Guid.TryParse(subject, out var userId))
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        var principal = await oidcGrantService.BuildPrincipalAsync(
            userId, authResult.Principal.GetScopes(), cancellationToken);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> HandleMfaOtpGrant(
        OpenIddictRequest request, CancellationToken cancellationToken)
    {
        var mfaToken = request.GetParameter("mfa_token")?.ToString();
        var otp = request.GetParameter("otp")?.ToString();
        var channelStr = request.GetParameter("mfa_channel")?.ToString();

        if (string.IsNullOrWhiteSpace(mfaToken) || string.IsNullOrWhiteSpace(otp)
            || string.IsNullOrWhiteSpace(channelStr))
            return OidcForbid(Errors.InvalidRequest,
                "The mfa_token, otp, and mfa_channel parameters are required.");

        if (!Guid.TryParse(mfaToken, out var challengeId)
            || !Enum.TryParse<TwoFactorChannel>(channelStr, true, out var channel))
            return OidcForbid(Errors.InvalidRequest,
                "The mfa_token or mfa_channel parameter is invalid.");

        ClaimsPrincipal principal;
        try
        {
            principal = await oidcGrantService.HandleMfaOtpGrantAsync(
                challengeId, channel, otp, request.GetScopes().ToList(), cancellationToken);
        }
        catch (AuthException)
        {
            return OidcForbid(Errors.InvalidGrant, "The MFA verification failed.");
        }

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> HandleTokenExchange(
        OpenIddictRequest request, CancellationToken cancellationToken)
    {
        var identitySource = request.GetParameter("identity_source")?.ToString();
        var token = request.GetParameter("token")?.ToString();
        var username = request.GetParameter("username")?.ToString();

        if (string.IsNullOrWhiteSpace(identitySource) || string.IsNullOrWhiteSpace(token))
            return OidcForbid(Errors.InvalidRequest,
                "The identity_source and token parameters are required.");

        PasswordGrantResult result;
        try
        {
            result = await identitySourceAuthService.AuthenticateAsync(
                identitySource, username, token, request.GetScopes().ToList(), cancellationToken);
        }
        catch (AuthException)
        {
            return OidcForbid(Errors.InvalidGrant, "The token exchange failed.");
        }

        return result switch
        {
            PasswordGrantResult.Success s =>
                SignIn(s.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme),

            PasswordGrantResult.MfaRequired mfa =>
                BadRequest(new
                {
                    error = "mfa_required",
                    error_description = "Multi-factor authentication is required.",
                    mfa_token = mfa.ChallengeId.ToString(),
                    mfa_channel = mfa.Channel.ToString().ToLowerInvariant()
                }),

            PasswordGrantResult.PasswordChangeRequired pc =>
                BadRequest(new
                {
                    error = "password_change_required",
                    error_description = "The user must change their password.",
                    challenge_id = pc.ChallengeId.ToString()
                }),

            _ => throw new InvalidOperationException($"Unexpected grant result: {result.GetType().Name}")
        };
    }

    private async Task<IActionResult> HandleClientCredentialsGrant(
        OpenIddictRequest request, CancellationToken cancellationToken)
    {
        var clientId = request.ClientId;
        if (string.IsNullOrWhiteSpace(clientId))
            return OidcForbid(Errors.InvalidRequest, "The client_id parameter is required.");

        ClaimsPrincipal principal;
        try
        {
            principal = await oidcGrantService.HandleClientCredentialsGrantAsync(
                clientId, request.GetScopes().ToList(), cancellationToken);
        }
        catch (AuthException)
        {
            return OidcForbid(Errors.InvalidGrant, "The client credentials are invalid.");
        }

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private IActionResult OidcForbid(string error, string description) =>
        Forbid(
            authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
            }));
}
