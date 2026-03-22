using MediatR;

namespace Auth.Application.Applications.Commands.CreateApplication;

public sealed record CreateApplicationCommand(
    string Name,
    string Description,
    bool IsActive = true,
    bool IsConfidential = true,
    string? LogoUrl = null,
    string? HomepageUrl = null,
    List<string>? RedirectUris = null,
    List<string>? PostLogoutRedirectUris = null,
    string? ConsentType = null,
    List<string>? Scopes = null) : IRequest<CreateApplicationResponse>;
