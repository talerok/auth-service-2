using MediatR;

namespace Auth.Application.Oidc.Commands.HandlePasswordGrant;

public sealed record HandlePasswordGrantCommand(
    string Username, string Password, IReadOnlyCollection<string> Scopes) : IRequest<PasswordGrantResult>;
