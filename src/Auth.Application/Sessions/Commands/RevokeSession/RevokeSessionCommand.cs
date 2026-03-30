using MediatR;

namespace Auth.Application.Sessions.Commands.RevokeSession;

public sealed record RevokeSessionCommand(Guid SessionId, string Reason) : IRequest;
