using Auth.Application;
using Auth.Application.Users.Queries.ExportUsers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Users.Queries.ExportUsers;

internal sealed class ExportUsersQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<ExportUsersQuery, IReadOnlyCollection<ExportUserDto>>
{
    public async Task<IReadOnlyCollection<ExportUserDto>> Handle(ExportUsersQuery query, CancellationToken cancellationToken)
    {
        var users = await dbContext.Users
            .Include(u => u.UserWorkspaces)
                .ThenInclude(uw => uw.Workspace)
            .Include(u => u.UserWorkspaces)
                .ThenInclude(uw => uw.UserWorkspaceRoles)
                    .ThenInclude(uwr => uwr.Role)
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.Id).ToList();

        var linksByUser = await dbContext.IdentitySourceLinks
            .Where(l => userIds.Contains(l.UserId))
            .Join(dbContext.IdentitySources, l => l.IdentitySourceId, s => s.Id,
                (l, s) => new { l.UserId, IdentitySourceCode = s.Code, l.ExternalIdentity })
            .ToListAsync(cancellationToken);

        var linksLookup = linksByUser
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<ExportUserIdentitySourceDto>)g
                .Select(x => new ExportUserIdentitySourceDto(x.IdentitySourceCode, x.ExternalIdentity))
                .OrderBy(x => x.IdentitySourceCode)
                .ToList());

        return users.Select(u => new ExportUserDto(
            u.Username,
            u.FullName,
            u.Email,
            u.Phone,
            u.IsActive,
            u.IsInternalAuthEnabled,
            u.MustChangePassword,
            u.TwoFactorEnabled,
            u.TwoFactorChannel,
            u.Locale,
            u.EmailVerified,
            u.PhoneVerified,
            u.UserWorkspaces
                .Where(uw => uw.Workspace is not null)
                .Select(uw => new ExportUserWorkspaceDto(
                    uw.Workspace!.Code,
                    uw.UserWorkspaceRoles
                        .Where(uwr => uwr.Role is not null)
                        .Select(uwr => uwr.Role!.Code)
                        .OrderBy(c => c)
                        .ToList()))
                .OrderBy(w => w.WorkspaceCode)
                .ToList(),
            linksLookup.TryGetValue(u.Id, out var links) ? links : []
        )).ToList();
    }
}
