using Auth.Application;
using Auth.Application.Auth.Commands.ValidateCredentials;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Authentication.Commands.ValidateCredentials;

internal sealed class ValidateCredentialsCommandHandler(
    AuthDbContext dbContext,
    IPasswordHasher passwordHasher,
    IAuditContext auditContext) : IRequestHandler<ValidateCredentialsCommand, User>
{
    public async Task<User> Handle(ValidateCredentialsCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var user = await dbContext.Users
                .FirstOrDefaultAsync(x => x.Username == command.Username, cancellationToken);

            if (user is null || !user.IsActive || !passwordHasher.Verify(command.Password, user.PasswordHash))
                throw new AuthException(AuthErrorCatalog.InvalidCredentials);

            if (!user.IsInternalAuthEnabled)
                throw new AuthException(AuthErrorCatalog.InternalAuthDisabled);

            auditContext.EntityId = user.Id;
            auditContext.Actor = new AuditActor(user.Id, user.FullName);
            auditContext.Details = new Dictionary<string, object?>
            {
                ["username"] = command.Username,
                ["result"] = "success"
            };

            return user;
        }
        catch (AuthException)
        {
            auditContext.Details = new Dictionary<string, object?>
            {
                ["username"] = command.Username,
                ["result"] = "failure"
            };
            throw;
        }
    }
}
