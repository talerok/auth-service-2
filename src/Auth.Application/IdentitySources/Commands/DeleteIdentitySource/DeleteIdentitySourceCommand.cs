using MediatR;

namespace Auth.Application.IdentitySources.Commands.DeleteIdentitySource;

public sealed record DeleteIdentitySourceCommand(Guid Id) : IRequest;
