using Auth.Application;
using Auth.Application.Auth.Commands.ValidateCredentials;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Authentication.Commands.ValidateCredentials;

internal sealed class ValidateCredentialsCommandHandler(
    AuthDbContext dbContext,
    IPasswordHasher passwordHasher) : IRequestHandler<ValidateCredentialsCommand, User>
{
    public async Task<User> Handle(ValidateCredentialsCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Username == command.Username, cancellationToken);

        if (user is null || !user.IsActive || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            throw new AuthException(AuthErrorCatalog.InvalidCredentials);
        }

        return user;
    }
}
