using Auth.Domain;
using MediatR;

namespace Auth.Application.Applications.Commands.UpdateApplication;

public sealed record UpdateApplicationCommand(
    Guid Id,
    string Name,
    string Description,
    bool IsActive,
    string? LogoUrl,
    string? HomepageUrl,
    List<string> RedirectUris,
    List<string> PostLogoutRedirectUris,
    List<string> AllowedOrigins,
    string? ConsentType,
    List<string> Scopes,
    List<string> GrantTypes,
    List<string> Audiences,
    int? AccessTokenLifetimeMinutes,
    int? RefreshTokenLifetimeMinutes) : IRequest<ApplicationDto?>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.Application;
    public AuditAction Action => AuditAction.Update;
    public Guid EntityId => Id;
}
