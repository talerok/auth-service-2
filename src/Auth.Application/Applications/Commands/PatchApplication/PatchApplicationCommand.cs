using Auth.Application.Common;
using Auth.Domain;
using MediatR;

namespace Auth.Application.Applications.Commands.PatchApplication;

public sealed record PatchApplicationCommand(
    Guid Id,
    Optional<string> Name,
    Optional<string> Description,
    Optional<bool> IsActive,
    Optional<string?> LogoUrl,
    Optional<string?> HomepageUrl,
    Optional<List<string>> RedirectUris,
    Optional<List<string>> PostLogoutRedirectUris,
    Optional<List<string>> AllowedOrigins,
    Optional<string> ConsentType,
    Optional<List<string>> Scopes,
    Optional<List<string>> GrantTypes,
    Optional<List<string>> Audiences,
    Optional<int?> AccessTokenLifetimeMinutes,
    Optional<int?> RefreshTokenLifetimeMinutes,
    Optional<bool> RequireEmailVerified,
    Optional<bool> RequirePhoneVerified) : IRequest<ApplicationDto?>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.Application;
    public AuditAction Action => AuditAction.Patch;
    public Guid EntityId => Id;
}
