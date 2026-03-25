using Auth.Application;
using Auth.Application.Users.Commands.ResetPassword;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Users.Commands.ResetPassword;

internal sealed class ResetPasswordCommandHandler(
    AuthDbContext dbContext,
    IPasswordHasher passwordHasher,
    IAuditContext auditContext) : IRequestHandler<ResetPasswordCommand, bool>
{
    public async Task<bool> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.UserId, cancellationToken);
        if (user is null)
        {
            auditContext.Details = new Dictionary<string, object?> { ["result"] = "user_not_found" };
            return false;
        }

        user.SetPassword(passwordHasher.Hash(command.NewPassword));
        user.MarkMustChangePassword();
        await dbContext.SaveChangesAsync(cancellationToken);

        auditContext.Details = new Dictionary<string, object?> { ["result"] = "success" };
        return true;
    }
}
