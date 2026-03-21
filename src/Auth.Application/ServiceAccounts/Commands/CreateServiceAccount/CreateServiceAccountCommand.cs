using MediatR;

namespace Auth.Application.ServiceAccounts.Commands.CreateServiceAccount;

public sealed record CreateServiceAccountCommand(
    string Name,
    string Description,
    bool IsActive = true) : IRequest<CreateServiceAccountResponse>;
