using MediatR;

namespace Auth.Application.ApiClients.Commands.UpdateApiClient;

public sealed record UpdateApiClientCommand(
    Guid Id,
    string Name,
    string Description,
    bool IsActive) : IRequest<ApiClientDto?>;
