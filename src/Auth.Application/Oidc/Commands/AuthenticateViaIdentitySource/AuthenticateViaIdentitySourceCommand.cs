using MediatR;

namespace Auth.Application.Oidc.Commands.AuthenticateViaIdentitySource;

public sealed record AuthenticateViaIdentitySourceCommand(
    string IdentitySourceName, string? Username, string Token,
    IReadOnlyCollection<string> Scopes) : IRequest<PasswordGrantResult>;
