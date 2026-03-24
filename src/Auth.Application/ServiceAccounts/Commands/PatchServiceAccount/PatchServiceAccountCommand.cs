using MediatR;

namespace Auth.Application.ServiceAccounts.Commands.PatchServiceAccount;

public sealed record PatchServiceAccountCommand(
    Guid Id,
    string? Name,
    string? Description,
    bool? IsActive,
    IReadOnlyCollection<string>? Audiences = null,
    int? AccessTokenLifetimeMinutes = null) : IRequest<ServiceAccountDto?>;
