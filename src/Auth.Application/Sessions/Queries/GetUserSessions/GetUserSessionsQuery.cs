using MediatR;

namespace Auth.Application.Sessions.Queries.GetUserSessions;

public sealed record GetUserSessionsQuery(
    Guid UserId, Guid? CurrentSessionId = null) : IRequest<IReadOnlyCollection<UserSessionResponse>>;
