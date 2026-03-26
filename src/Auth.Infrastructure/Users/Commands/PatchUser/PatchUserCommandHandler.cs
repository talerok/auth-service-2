using Auth.Application;
using Auth.Application.Users.Commands.PatchUser;
using Auth.Domain;
using Auth.Infrastructure.AuditLogs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Users.Commands.PatchUser;

internal sealed class PatchUserCommandHandler(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    IAuditContext auditContext) : IRequestHandler<PatchUserCommand, UserDto?>
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
        {
            if (command.IsActive.Value) user.Activate(); else user.Deactivate();
        }

        if (command.IsInternalAuthEnabled.HasValue)
            user.IsInternalAuthEnabled = command.IsInternalAuthEnabled.Value;

        if (command.TwoFactorEnabled.HasValue)
        {
            if (command.TwoFactorEnabled.Value)
                user.EnableTwoFactor(command.TwoFactorChannel ?? TwoFactorChannel.Email);
            else
                user.DisableTwoFactor();
        }

        if (command.Locale is not null)
            user.Locale = command.Locale;

        if (command.EmailVerified.HasValue)
            user.EmailVerified = command.EmailVerified.Value;

        if (command.PhoneVerified.HasValue)
            user.PhoneVerified = command.PhoneVerified.Value;

        var changes = AuditDiff.CaptureChanges(dbContext.Entry(user));
        if (changes.Count > 0)
            auditContext.Details = changes;

        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = new UserDto(user.Id, user.Username, user.FullName, user.Email, user.Phone,
            user.IsActive, user.IsInternalAuthEnabled, user.MustChangePassword, user.TwoFactorEnabled, user.TwoFactorChannel,
            user.Locale, user.EmailVerified, user.PhoneVerified);

        await searchIndexService.IndexUserAsync(dto, cancellationToken);
        return dto;
    }
}
