using MediatR;

namespace Auth.Application.ServiceAccounts.Commands.RegenerateServiceAccountSecret;

public sealed record RegenerateServiceAccountSecretCommand(Guid Id) : IRequest<RegenerateServiceAccountSecretResponse?>;
