using Auth.Application;
using Auth.Application.Auth.Commands.ValidateForcedPasswordChange;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.Authentication.Commands.ValidateForcedPasswordChange;

internal sealed class ValidateForcedPasswordChangeCommandHandler(
    AuthDbContext dbContext,
    IPasswordHasher passwordHasher,
    IAuditContext auditContext,
    ILogger<ValidateForcedPasswordChangeCommandHandler> logger) : IRequestHandler<ValidateForcedPasswordChangeCommand, User>
{
    public async Task<User> Handle(ValidateForcedPasswordChangeCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var passwordChallenge = await dbContext.PasswordChangeChallenges
                .FirstOrDefaultAsync(x => x.Id == command.ChallengeId, cancellationToken);

            if (passwordChallenge is null || passwordChallenge.IsUsed || passwordChallenge.IsExpired(DateTime.UtcNow))
                throw new AuthException(AuthErrorCatalog.InvalidPasswordChangeChallenge);

            var user = await dbContext.Users
                .FirstOrDefaultAsync(x => x.Id == passwordChallenge.UserId, cancellationToken);

            if (user is null || !user.IsActive)
                throw new AuthException(AuthErrorCatalog.UserInactive);

            user.SetPassword(passwordHasher.Hash(command.NewPassword));
            user.ClearMustChangePassword();
            passwordChallenge.MarkAsUsed();
            await dbContext.SaveChangesAsync(cancellationToken);

            auditContext.EntityId = user.Id;
            auditContext.Details = new Dictionary<string, object?> { ["result"] = "success" };

            logger.LogInformation(
                "PasswordChangeOperation userId={UserId} operation={Operation} result={Result}",
                user.Id, "FORCED_PASSWORD_CHANGED", "SUCCESS");

            return user;
        }
        catch (AuthException)
        {
            auditContext.Details = new Dictionary<string, object?> { ["result"] = "failure" };
            throw;
        }
    }
}
