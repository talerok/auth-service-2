using MediatR;

namespace Auth.Application.Sessions.Commands.TouchSession;

public sealed record TouchSessionCommand(Guid SessionId, Guid UserId) : IRequest;
