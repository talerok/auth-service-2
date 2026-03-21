using MediatR;

namespace Auth.Application.Applications.Commands.PatchApplication;

public sealed record PatchApplicationCommand(
    Guid Id,
    string? Name,
    string? Description,
    bool? IsActive,
    bool? IsConfidential,
    string? LogoUrl,
    string? HomepageUrl,
    List<string>? RedirectUris,
    List<string>? PostLogoutRedirectUris,
    string? ConsentType) : IRequest<ApplicationDto?>;
