using MediatR;

namespace Auth.Application.Applications.Commands.UpdateApplication;

public sealed record UpdateApplicationCommand(
    Guid Id,
    string Name,
    string Description,
    bool IsActive,
    bool IsConfidential,
    string? LogoUrl,
    string? HomepageUrl,
    List<string> RedirectUris,
    List<string> PostLogoutRedirectUris,
    string? ConsentType) : IRequest<ApplicationDto?>;
