using MediatR;

namespace Auth.Application.Oidc.Queries.GetApplicationAudiences;

public sealed record GetApplicationAudiencesQuery(string ClientId) : IRequest<IReadOnlyList<string>>;
