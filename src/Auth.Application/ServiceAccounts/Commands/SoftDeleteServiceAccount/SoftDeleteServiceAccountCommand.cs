using MediatR;

namespace Auth.Application.ServiceAccounts.Commands.SoftDeleteServiceAccount;

public sealed record SoftDeleteServiceAccountCommand(Guid Id) : IRequest<bool>;
