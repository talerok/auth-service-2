using System.Security.Claims;
using MediatR;

namespace Auth.Application.Oidc.Queries.BuildPrincipal;

public sealed record BuildPrincipalQuery(
    Guid UserId, IReadOnlyCollection<string> Scopes, string? ClientId = null,
    IReadOnlyList<string>? AuthMethods = null,
    Guid? SessionId = null) : IRequest<ClaimsPrincipal>;
