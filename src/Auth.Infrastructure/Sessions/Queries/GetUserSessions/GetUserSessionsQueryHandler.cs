using Auth.Application.Sessions;
using Auth.Application.Sessions.Queries.GetUserSessions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Sessions.Queries.GetUserSessions;

internal sealed class GetUserSessionsQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetUserSessionsQuery, IReadOnlyCollection<UserSessionResponse>>
{
    public async Task<IReadOnlyCollection<UserSessionResponse>> Handle(GetUserSessionsQuery query, CancellationToken cancellationToken)
    {
        var sessions = await dbContext.UserSessions
            .AsNoTracking()
            .Where(s => s.UserId == query.UserId && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new UserSessionResponse(
                s.Id,
                s.UserId,
                s.ApplicationId,
                s.Application != null ? s.Application.Name : null,
                s.IpAddress,
                s.UserAgent,
                s.AuthMethod,
                s.IsRevoked,
                query.CurrentSessionId.HasValue && s.Id == query.CurrentSessionId.Value && !s.IsRevoked,
                s.CreatedAt,
                s.ExpiresAt,
                s.LastActivityAt,
                s.RevokedAt,
                s.RevokedReason))
            .ToListAsync(cancellationToken);

        return sessions;
    }
}
