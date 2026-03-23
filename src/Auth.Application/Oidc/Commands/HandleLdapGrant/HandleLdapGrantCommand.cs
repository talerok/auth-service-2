using MediatR;

namespace Auth.Application.Oidc.Commands.HandleLdapGrant;

public sealed record HandleLdapGrantCommand(
    string? IdentitySource, string? Username, string? Password,
    IReadOnlyCollection<string> Scopes) : IRequest<CredentialValidationResult>;
