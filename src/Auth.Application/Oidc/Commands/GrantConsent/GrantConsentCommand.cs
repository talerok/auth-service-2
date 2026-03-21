using MediatR;

namespace Auth.Application.Oidc.Commands.GrantConsent;

public sealed record GrantConsentCommand(
    string ClientId,
    Guid UserId,
    IReadOnlyCollection<string> Scopes) : IRequest<string>;
