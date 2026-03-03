using Auth.Application;
using Auth.Application.Auth.Queries.GetActiveUser;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Authentication.Queries.GetActiveUser;

internal sealed class GetActiveUserQueryHandler(
    AuthDbContext dbContext) : IRequestHandler<GetActiveUserQuery, User>
{
    public async Task<User> Handle(GetActiveUserQuery query, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == query.UserId, cancellationToken);

        if (user is null || !user.IsActive)
            throw new AuthException(AuthErrorCatalog.UserInactive);

        return user;
    }
}
