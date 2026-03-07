using Auth.Application;
using Auth.Application.Users.Queries.GetUserById;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Users.Queries.GetUserById;

internal sealed class GetUserByIdQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    public async Task<UserDto?> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        return await dbContext.Users.AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new UserDto(x.Id, x.Username, x.FullName, x.Email, x.Phone,
                x.IsActive, x.IsInternalAuthEnabled, x.MustChangePassword, x.TwoFactorEnabled, x.TwoFactorChannel))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
