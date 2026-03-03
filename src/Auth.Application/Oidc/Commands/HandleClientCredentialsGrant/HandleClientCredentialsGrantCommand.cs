using System.Security.Claims;
using MediatR;

namespace Auth.Application.Oidc.Commands.HandleClientCredentialsGrant;

public sealed record HandleClientCredentialsGrantCommand(
    string ClientId, IReadOnlyCollection<string> Scopes) : IRequest<ClaimsPrincipal>;
