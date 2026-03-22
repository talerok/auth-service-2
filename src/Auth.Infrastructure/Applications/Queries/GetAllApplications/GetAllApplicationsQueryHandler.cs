using Auth.Application;
using Auth.Application.Applications.Queries.GetAllApplications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Applications.Queries.GetAllApplications;

internal sealed class GetAllApplicationsQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetAllApplicationsQuery, IReadOnlyCollection<ApplicationDto>>
{
    public async Task<IReadOnlyCollection<ApplicationDto>> Handle(GetAllApplicationsQuery query, CancellationToken cancellationToken)
    {
        return await dbContext.Applications.AsNoTracking()
            .Select(x => new ApplicationDto(
                x.Id, x.Name, x.Description, x.ClientId, x.IsActive,
                x.IsConfidential, x.LogoUrl, x.HomepageUrl,
                x.RedirectUris, x.PostLogoutRedirectUris, x.Scopes))
            .ToListAsync(cancellationToken);
    }
}
