using MediatR;

namespace Auth.Application.ApiClients.Commands.CreateApiClient;

public sealed record CreateApiClientCommand(
    string Name,
    string Description,
    bool IsActive = true) : IRequest<CreateApiClientResponse>;
