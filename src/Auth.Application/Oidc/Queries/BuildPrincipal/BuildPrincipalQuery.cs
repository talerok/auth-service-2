using System.Security.Claims;
using MediatR;

namespace Auth.Application.Oidc.Queries.BuildPrincipal;

public sealed record BuildPrincipalQuery(Guid UserId, IReadOnlyCollection<string> Scopes) : IRequest<ClaimsPrincipal>;
