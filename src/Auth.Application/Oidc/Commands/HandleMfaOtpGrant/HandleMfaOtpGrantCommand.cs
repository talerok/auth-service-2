using System.Security.Claims;
using MediatR;

namespace Auth.Application.Oidc.Commands.HandleMfaOtpGrant;

public sealed record HandleMfaOtpGrantCommand(
    string? MfaToken, string? MfaChannel, string? Otp,
    IReadOnlyCollection<string> Scopes) : IRequest<ClaimsPrincipal>;
