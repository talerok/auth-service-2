using MediatR;

namespace Auth.Application.ServiceAccounts.Commands.PatchServiceAccount;

public sealed record PatchServiceAccountCommand(
    Guid Id,
    string? Name,
    string? Description,
    bool? IsActive) : IRequest<ServiceAccountDto?>;
