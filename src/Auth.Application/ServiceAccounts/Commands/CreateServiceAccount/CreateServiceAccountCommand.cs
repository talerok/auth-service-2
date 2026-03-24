using MediatR;

namespace Auth.Application.ServiceAccounts.Commands.CreateServiceAccount;

public sealed record CreateServiceAccountCommand(
    string Name,
    string Description,
    bool IsActive = true,
    IReadOnlyCollection<string>? Audiences = null,
    int? AccessTokenLifetimeMinutes = null) : IRequest<CreateServiceAccountResponse>;
