using Auth.Application;
using Auth.Application.Users.Queries.GetAllUsers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Users.Queries.GetAllUsers;

internal sealed class GetAllUsersQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetAllUsersQuery, IReadOnlyCollection<UserDto>>
{
    public async Task<IReadOnlyCollection<UserDto>> Handle(GetAllUsersQuery query, CancellationToken cancellationToken)
    {
        return await dbContext.Users.AsNoTracking()
            .Select(x => new UserDto(x.Id, x.Username, x.FullName, x.Email, x.Phone,
                x.IsActive, x.IsInternalAuthEnabled, x.MustChangePassword, x.TwoFactorEnabled, x.TwoFactorChannel))
            .ToListAsync(cancellationToken);
    }
}
