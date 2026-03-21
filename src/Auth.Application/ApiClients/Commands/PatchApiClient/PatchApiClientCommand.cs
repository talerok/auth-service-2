using Auth.Domain;
using MediatR;

namespace Auth.Application.ApiClients.Commands.PatchApiClient;

public sealed record PatchApiClientCommand(
    Guid Id,
    string? Name,
    string? Description,
    bool? IsActive,
    ApiClientType? Type,
    bool? IsConfidential,
    string? LogoUrl,
    string? HomepageUrl,
    List<string>? RedirectUris,
    List<string>? PostLogoutRedirectUris,
    string? ConsentType) : IRequest<ApiClientDto?>;
