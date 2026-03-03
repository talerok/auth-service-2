using System.Security.Claims;
using Auth.Domain;
using MediatR;

namespace Auth.Application.Oidc.Commands.HandleMfaOtpGrant;

public sealed record HandleMfaOtpGrantCommand(
    Guid ChallengeId, TwoFactorChannel Channel, string Otp,
    IReadOnlyCollection<string> Scopes) : IRequest<ClaimsPrincipal>;
