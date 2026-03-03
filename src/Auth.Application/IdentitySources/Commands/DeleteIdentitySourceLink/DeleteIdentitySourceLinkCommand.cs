using MediatR;

namespace Auth.Application.IdentitySources.Commands.DeleteIdentitySourceLink;

public sealed record DeleteIdentitySourceLinkCommand(
    Guid IdentitySourceId,
    Guid LinkId) : IRequest;
