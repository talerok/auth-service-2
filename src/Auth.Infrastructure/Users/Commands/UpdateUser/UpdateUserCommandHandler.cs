using Auth.Application;
using Auth.Application.Users.Commands.UpdateUser;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Users.Commands.UpdateUser;

internal sealed class UpdateUserCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService) : IRequestHandler<UpdateUserCommand, UserDto?>
{
    public async Task<UserDto?> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (user is null)
            return null;

        user.Username = command.Username;
        user.FullName = command.FullName;
        user.Email = command.Email;
        user.Phone = command.Phone;
        if (command.IsActive) user.Activate(); else user.Deactivate();
        user.IsInternalAuthEnabled = command.IsInternalAuthEnabled;

        if (command.TwoFactorEnabled)
            user.EnableTwoFactor(command.TwoFactorChannel ?? TwoFactorChannel.Email);
        else
            user.DisableTwoFactor();

        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = new UserDto(user.Id, user.Username, user.FullName, user.Email, user.Phone,
            user.IsActive, user.IsInternalAuthEnabled, user.MustChangePassword, user.TwoFactorEnabled, user.TwoFactorChannel);

        await searchIndexService.IndexUserAsync(dto, cancellationToken);
        return dto;
    }
}
