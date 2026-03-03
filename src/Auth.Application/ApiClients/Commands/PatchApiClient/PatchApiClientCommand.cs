using MediatR;

namespace Auth.Application.ApiClients.Commands.PatchApiClient;

public sealed record PatchApiClientCommand(
    Guid Id,
    string? Name,
    string? Description,
    bool? IsActive) : IRequest<ApiClientDto?>;
