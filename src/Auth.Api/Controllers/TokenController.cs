using System.Security.Claims;
using Auth.Api.RateLimit;
using Auth.Application;
using Auth.Application.Oidc.Commands.HandleClientCredentialsGrant;
using Auth.Application.Oidc.Commands.HandleJwtBearerGrant;
using Auth.Application.Oidc.Commands.HandleLdapGrant;
using Auth.Application.Oidc.Commands.HandleMfaOtpGrant;
using Auth.Application.Oidc.Commands.ValidateCredentialsForLogin;
using Auth.Application.Oidc.Queries.BuildPrincipal;
using Auth.Application.Sessions.Commands.TouchSession;
using MediatR;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.Api.Controllers;

[ApiController]
[EnableCors("Oidc")]
[Route("")]
[RateLimit(RateLimitPolicies.Auth)]
public sealed class TokenController(ISender sender) : ControllerBase
{
    // ─── Token Exchange ──────────────────────────────────────────────

    [HttpPost("connect/token")]
    public async Task<IActionResult> Exchange(CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsAuthorizationCodeGrantType())
            return await HandleSubjectGrant("The authorization code is no longer valid.", request.ClientId, cancellationToken);

        if (request.IsRefreshTokenGrantType())
            return await HandleSubjectGrant("The refresh token is no longer valid.", request.ClientId, cancellationToken);

        if (request.IsPasswordGrantType())
            return await HandlePasswordGrant(request, cancellationToken);

        if (request.GrantType == OidcConstants.MfaOtpGrantType)
            return await HandleMfaOtpGrant(request, cancellationToken);

        if (request.GrantType == OidcConstants.JwtBearerGrantType)
            return await HandleJwtBearerGrant(request, cancellationToken);

        if (request.GrantType == OidcConstants.LdapGrantType)
            return await HandleLdapGrant(request, cancellationToken);

        if (request.IsClientCredentialsGrantType())
            return await HandleClientCredentialsGrant(request, cancellationToken);

        return OidcForbid(Errors.UnsupportedGrantType, "The specified grant type is not supported.");
    }

    // ─── Grant Handlers ──────────────────────────────────────────────

    private async Task<IActionResult> HandleSubjectGrant(
        string errorDescription, string? clientId, CancellationToken cancellationToken)
    {
        var authResult = await HttpContext.AuthenticateAsync(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        if (!authResult.Succeeded || authResult.Principal is null)
            return OidcForbid(Errors.InvalidGrant, errorDescription);

        var subject = authResult.Principal.FindFirst(Claims.Subject)?.Value;
        if (!Guid.TryParse(subject, out var userId))
            return OidcForbid(Errors.InvalidGrant, "The user identifier is invalid.");

        var authMethods = authResult.Principal
            .FindAll(Claims.AuthenticationMethodReference)
            .Select(c => c.Value).ToList();

        var sidClaim = authResult.Principal.FindFirst("sid")?.Value;
        if (!Guid.TryParse(sidClaim, out var sessionId))
            return OidcForbid(Errors.InvalidGrant, "The session identifier is missing.");

        try
        {
            await sender.Send(new TouchSessionCommand(sessionId, userId), cancellationToken);
        }
        catch (AuthException)
        {
            return OidcForbid(Errors.InvalidGrant, "The session has been revoked.");
        }

        var principal = await sender.Send(new BuildPrincipalQuery(
            userId, authResult.Principal.GetScopes().ToList(), clientId, authMethods, sessionId), cancellationToken);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> HandlePasswordGrant(
        OpenIddictRequest request, CancellationToken cancellationToken)
    {
        CredentialValidationResult result;
        try
        {
            result = await sender.Send(new ValidateCredentialsForLoginCommand(
                request.Username!, request.Password!, request.GetScopes().ToList(), request.ClientId), cancellationToken);
        }
        catch (AuthException)
        {
            return OidcForbid(Errors.InvalidGrant, "The credentials are invalid.");
        }

        return ToActionResult(result);
    }

    private async Task<IActionResult> HandleMfaOtpGrant(
        OpenIddictRequest request, CancellationToken cancellationToken)
    {
        ClaimsPrincipal principal;
        try
        {
            principal = await sender.Send(new HandleMfaOtpGrantCommand(
                request.GetParameter("mfa_token")?.ToString(),
                request.GetParameter("mfa_channel")?.ToString(),
                request.GetParameter("otp")?.ToString(),
                request.GetScopes().ToList(), request.ClientId), cancellationToken);
        }
        catch (AuthException)
        {
            return OidcForbid(Errors.InvalidGrant, "The MFA verification failed.");
        }

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> HandleJwtBearerGrant(
        OpenIddictRequest request, CancellationToken cancellationToken)
    {
        CredentialValidationResult result;
        try
        {
            result = await sender.Send(new HandleJwtBearerGrantCommand(
                request.GetParameter("assertion")?.ToString(),
                request.GetScopes().ToList(), request.ClientId), cancellationToken);
        }
        catch (AuthException)
        {
            return OidcForbid(Errors.InvalidGrant, "The JWT assertion is invalid.");
        }

        return ToActionResult(result);
    }

    private async Task<IActionResult> HandleLdapGrant(
        OpenIddictRequest request, CancellationToken cancellationToken)
    {
        CredentialValidationResult result;
        try
        {
            result = await sender.Send(new HandleLdapGrantCommand(
                request.GetParameter("identity_source")?.ToString(),
                request.GetParameter("username")?.ToString(),
                request.GetParameter("password")?.ToString(),
                request.GetScopes().ToList(), request.ClientId), cancellationToken);
        }
        catch (AuthException)
        {
            return OidcForbid(Errors.InvalidGrant, "The LDAP authentication failed.");
        }

        return ToActionResult(result);
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
            principal = await sender.Send(new HandleClientCredentialsGrantCommand(
                clientId, request.GetScopes().ToList()), cancellationToken);
        }
        catch (AuthException)
        {
            return OidcForbid(Errors.InvalidGrant, "The client credentials are invalid.");
        }

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    // ─── Utilities ───────────────────────────────────────────────────

    private IActionResult ToActionResult(CredentialValidationResult result) => result switch
    {
        CredentialValidationResult.Success s =>
            SignIn(s.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme),

        CredentialValidationResult.MfaRequired mfa =>
            BadRequest(new
            {
                error = "mfa_required",
                error_description = "Multi-factor authentication is required.",
                mfa_token = mfa.ChallengeId.ToString(),
                mfa_channel = mfa.Channel.ToString().ToLowerInvariant()
            }),

        CredentialValidationResult.PasswordChangeRequired pc =>
            BadRequest(new
            {
                error = "password_change_required",
                error_description = "The user must change their password.",
                challenge_id = pc.ChallengeId.ToString()
            }),

        _ => throw new InvalidOperationException($"Unexpected grant result: {result.GetType().Name}")
    };

    private IActionResult OidcForbid(string error, string description) =>
        Forbid(
            authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
            }));
}
