using MediatR;

namespace Auth.Application.Oidc.Queries.GetUserInfo;

public sealed record GetUserInfoQuery(
    Guid UserId, IReadOnlyCollection<string> Scopes) : IRequest<Dictionary<string, object>>;
