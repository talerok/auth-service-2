using Auth.Application;
using Auth.Application.Applications.Queries.GetApplicationById;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Applications.Queries.GetApplicationById;

internal sealed class GetApplicationByIdQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetApplicationByIdQuery, ApplicationDto?>
{
    public async Task<ApplicationDto?> Handle(GetApplicationByIdQuery query, CancellationToken cancellationToken)
    {
        return await dbContext.Applications.AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new ApplicationDto(
                x.Id, x.Name, x.Description, x.ClientId, x.IsActive,
                x.IsConfidential, x.LogoUrl, x.HomepageUrl,
                x.RedirectUris, x.PostLogoutRedirectUris, x.AllowedOrigins, x.Scopes,
                x.GrantTypes, x.Audiences, x.AccessTokenLifetimeMinutes, x.RefreshTokenLifetimeMinutes,
                x.RequireEmailVerified, x.RequirePhoneVerified))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
