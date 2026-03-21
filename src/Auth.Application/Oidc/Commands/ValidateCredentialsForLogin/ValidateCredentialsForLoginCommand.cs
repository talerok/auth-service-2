using MediatR;

namespace Auth.Application.Oidc.Commands.ValidateCredentialsForLogin;

public sealed record ValidateCredentialsForLoginCommand(
    string Username, string Password, IReadOnlyCollection<string> Scopes) : IRequest<CredentialValidationResult>;
