using MediatR;

namespace Auth.Application.Sessions.Commands.CreateSession;

public sealed record CreateSessionCommand(
    Guid UserId, string? ClientId, string AuthMethod,
    string IpAddress, string UserAgent) : IRequest<Guid>;
