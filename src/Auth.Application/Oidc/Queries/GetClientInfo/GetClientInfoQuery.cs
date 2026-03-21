using MediatR;

namespace Auth.Application.Oidc.Queries.GetClientInfo;

public sealed record GetClientInfoQuery(string ClientId) : IRequest<ClientInfoResult?>;

public sealed record ClientInfoResult(
    string Name,
    string? LogoUrl,
    string? HomepageUrl);
