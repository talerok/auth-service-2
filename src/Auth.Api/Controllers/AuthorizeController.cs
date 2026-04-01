using Auth.Application.Oidc.Commands.GrantConsent;
using Auth.Application.Oidc.Queries.BuildPrincipal;
using Auth.Application.Oidc.Queries.ResolveAuthorizeRequest;
using Auth.Infrastructure;
using MediatR;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.Api.Controllers;

[ApiController]
[EnableCors("Oidc")]
[Route("")]
public sealed class AuthorizeController(
    ISender sender,
    IOptions<IntegrationOptions> integrationOptions) : ControllerBase
{
    private const string CookieScheme = "Identity.External";

    // ─── Authorization Code Flow ─────────────────────────────────────

    [HttpGet("connect/authorize")]
    [HttpPost("connect/authorize")]
    public async Task<IActionResult> Authorize(CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        var returnUrl = Request.PathBase + Request.Path + QueryString.Create(
            Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList());

        var authResult = await HttpContext.AuthenticateAsync(CookieScheme);

        if (!authResult.Succeeded || authResult.Principal is null)
        {
            return Challenge(
                authenticationSchemes: [CookieScheme],
                properties: new AuthenticationProperties { RedirectUri = returnUrl });
        }

        var subject = authResult.Principal.FindFirst(Claims.Subject)?.Value;
        if (!Guid.TryParse(subject, out var userId))
            return OidcForbid(Errors.InvalidGrant, "The user identifier is invalid.");

        if (Request.Query.ContainsKey("consent_denied"))
            return OidcForbid(Errors.AccessDenied, "The user denied the consent request.");

        var resolveResult = await sender.Send(new ResolveAuthorizeRequestQuery(
            request.ClientId!, userId, request.GetScopes().ToList()), cancellationToken);

        if (resolveResult.EmailVerificationRequired || resolveResult.PhoneVerificationRequired)
        {
            var parts = new List<string>();
            if (resolveResult.EmailVerificationRequired) parts.Add("email");
            if (resolveResult.PhoneVerificationRequired) parts.Add("phone");
            return OidcForbid(Errors.AccessDenied, $"{string.Join(" and ", parts)} verification is required.");
        }

        if (resolveResult.ConsentRequired)
        {
            var oidcOptions = integrationOptions.Value.Oidc;

            return Redirect(
                $"{oidcOptions.ConsentUrl}" +
                $"?client_id={Uri.EscapeDataString(request.ClientId!)}" +
                $"&scope={Uri.EscapeDataString(string.Join(" ", request.GetScopes()))}" +
                $"&return_url={Uri.EscapeDataString(returnUrl)}");
        }

        var scopes = request.GetScopes().ToList();
        var authorizationId = resolveResult.AuthorizationId
            ?? await sender.Send(new GrantConsentCommand(request.ClientId!, userId, scopes), cancellationToken);

        var authMethods = authResult.Principal
            .FindAll(Claims.AuthenticationMethodReference)
            .Select(c => c.Value).ToList();

        var principal = await sender.Send(new BuildPrincipalQuery(
            userId, scopes, request.ClientId, authMethods), cancellationToken);

        principal.SetAuthorizationId(authorizationId);

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
