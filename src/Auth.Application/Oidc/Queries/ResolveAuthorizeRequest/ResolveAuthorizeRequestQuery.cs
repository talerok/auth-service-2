using MediatR;

namespace Auth.Application.Oidc.Queries.ResolveAuthorizeRequest;

public sealed record ResolveAuthorizeRequestQuery(
    string ClientId,
    Guid UserId,
    IReadOnlyCollection<string> Scopes) : IRequest<AuthorizeRequestResult>;

public sealed record AuthorizeRequestResult(
    string? AuthorizationId,
    bool ConsentRequired);
