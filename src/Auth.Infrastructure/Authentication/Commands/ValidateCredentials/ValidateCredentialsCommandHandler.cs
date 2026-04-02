using Auth.Application;
using Auth.Application.Auth.Commands.ValidateCredentials;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Authentication.Commands.ValidateCredentials;

internal sealed class ValidateCredentialsCommandHandler(
    AuthDbContext dbContext,
    IPasswordHasher passwordHasher,
    IAuditContext auditContext,
    IOptions<IntegrationOptions> options) : IRequestHandler<ValidateCredentialsCommand, User>
{
    public async Task<User> Handle(ValidateCredentialsCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var user = await dbContext.Users
                .FirstOrDefaultAsync(x => x.Username == command.Username, cancellationToken);

            if (user is null || !user.IsActive)
                throw new AuthException(AuthErrorCatalog.InvalidCredentials);

            if (user.IsLockedOut)
                throw new AuthException(AuthErrorCatalog.AccountLockedOut);

            if (!passwordHasher.Verify(command.Password, user.PasswordHash))
            {
                var lockout = options.Value.AccountLockout;
                user.RegisterFailedLogin(lockout.MaxFailedAttempts, lockout.LockoutDurationMinutes);
                await dbContext.SaveChangesAsync(cancellationToken);
                throw new AuthException(AuthErrorCatalog.InvalidCredentials);
            }

            if (!user.IsInternalAuthEnabled)
                throw new AuthException(AuthErrorCatalog.InternalAuthDisabled);

            if (user.FailedLoginAttempts > 0)
            {
                user.ResetFailedLoginAttempts();
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            auditContext.EntityId = user.Id;
            auditContext.Actor = new AuditActor(user.Id, user.FullName);
            auditContext.Details = new Dictionary<string, object?>
            {
                ["username"] = command.Username,
                ["result"] = "success"
            };

            return user;
        }
        catch (AuthException ex)
        {
            auditContext.Details = new Dictionary<string, object?>
            {
                ["username"] = command.Username,
                ["result"] = "failure",
                ["lockout"] = ex.Code == AuthErrorCatalog.AccountLockedOut
            };
            throw;
        }
    }
}
