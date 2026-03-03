using MediatR;

namespace Auth.Application.IdentitySources.Commands.CreateIdentitySourceLink;

public sealed record CreateIdentitySourceLinkCommand(
    Guid IdentitySourceId,
    Guid UserId,
    string ExternalIdentity) : IRequest<IdentitySourceLinkDto>;
