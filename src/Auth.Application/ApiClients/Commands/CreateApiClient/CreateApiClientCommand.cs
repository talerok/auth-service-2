using Auth.Domain;
using MediatR;

namespace Auth.Application.ApiClients.Commands.CreateApiClient;

public sealed record CreateApiClientCommand(
    string Name,
    string Description,
    bool IsActive = true,
    ApiClientType Type = ApiClientType.ServiceAccount,
    bool IsConfidential = true,
    string? LogoUrl = null,
    string? HomepageUrl = null,
    List<string>? RedirectUris = null,
    List<string>? PostLogoutRedirectUris = null,
    string? ConsentType = null) : IRequest<CreateApiClientResponse>;
