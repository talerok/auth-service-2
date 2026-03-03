using Auth.Application;
using Auth.Application.Users.Commands.PatchUser;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Users.Commands.PatchUser;

internal sealed class PatchUserCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService) : IRequestHandler<PatchUserCommand, UserDto?>
{
    public async Task<UserDto?> Handle(PatchUserCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (user is null)
            return null;

        if (command.Username is not null)
            user.Username = command.Username;

        if (command.FullName is not null)
            user.FullName = command.FullName;

        if (command.Email is not null)
            user.Email = command.Email;

        if (command.Phone is not null)
            user.Phone = command.Phone;

        if (command.IsActive.HasValue)
            user.IsActive = command.IsActive.Value;

        if (command.TwoFactorEnabled.HasValue)
        {
            if (command.TwoFactorEnabled.Value)
                user.EnableTwoFactor(command.TwoFactorChannel ?? TwoFactorChannel.Email);
            else
                user.DisableTwoFactor();
        }

        user.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = new UserDto(user.Id, user.Username, user.FullName, user.Email, user.Phone,
            user.IsActive, user.MustChangePassword, user.TwoFactorEnabled, user.TwoFactorChannel);

        await searchIndexService.IndexUserAsync(dto, cancellationToken);
        return dto;
    }
}
