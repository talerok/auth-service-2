using MediatR;

namespace Auth.Application.Oidc.Commands.HandleJwtBearerGrant;

public sealed record HandleJwtBearerGrantCommand(
    string? Assertion, IReadOnlyCollection<string> Scopes,
    string? ClientId = null) : IRequest<CredentialValidationResult>;
