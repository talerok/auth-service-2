using MediatR;

namespace Auth.Application.Applications.Commands.RegenerateApplicationSecret;

public sealed record RegenerateApplicationSecretCommand(Guid Id) : IRequest<RegenerateApplicationSecretResponse?>;
