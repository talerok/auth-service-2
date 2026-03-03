using MediatR;

namespace Auth.Application.ApiClients.Commands.SoftDeleteApiClient;

public sealed record SoftDeleteApiClientCommand(Guid Id) : IRequest<bool>;
