using MediatR;

namespace Auth.Application.Sessions.Commands.RevokeUserSessions;

public sealed record RevokeUserSessionsCommand(Guid UserId, string Reason) : IRequest;
