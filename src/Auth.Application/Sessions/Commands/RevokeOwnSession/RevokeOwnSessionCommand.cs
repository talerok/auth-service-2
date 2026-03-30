using MediatR;

namespace Auth.Application.Sessions.Commands.RevokeOwnSession;

public sealed record RevokeOwnSessionCommand(Guid SessionId, Guid UserId) : IRequest;
