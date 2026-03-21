using MediatR;

namespace Auth.Application.ServiceAccounts.Commands.UpdateServiceAccount;

public sealed record UpdateServiceAccountCommand(
    Guid Id,
    string Name,
    string Description,
    bool IsActive) : IRequest<ServiceAccountDto?>;
