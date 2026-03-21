using Auth.Domain;
using MediatR;

namespace Auth.Application.ApiClients.Commands.UpdateApiClient;

public sealed record UpdateApiClientCommand(
    Guid Id,
    string Name,
    string Description,
    bool IsActive,
    ApiClientType Type,
    bool IsConfidential,
    string? LogoUrl,
    string? HomepageUrl,
    List<string> RedirectUris,
    List<string> PostLogoutRedirectUris,
    string? ConsentType) : IRequest<ApiClientDto?>;
