using Auth.Application;
using Auth.Application.Users.Commands.ResetPassword;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Users.Commands.ResetPassword;

internal sealed class ResetPasswordCommandHandler(
    AuthDbContext dbContext,
    IPasswordHasher passwordHasher) : IRequestHandler<ResetPasswordCommand, bool>
{
    public async Task<bool> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.UserId, cancellationToken);
        if (user is null)
            return false;

        user.PasswordHash = passwordHasher.Hash(command.NewPassword);
        user.MarkMustChangePassword();
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
