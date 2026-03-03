using MediatR;

namespace Auth.Application.ApiClients.Commands.RegenerateApiClientSecret;

public sealed record RegenerateApiClientSecretCommand(Guid Id) : IRequest<RegenerateApiClientSecretResponse?>;
