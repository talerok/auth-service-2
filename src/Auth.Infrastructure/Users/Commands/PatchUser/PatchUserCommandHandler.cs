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

        if (command.Username.HasValue)
            user.Username = command.Username.Value!;

        if (command.FullName.HasValue)
            user.FullName = command.FullName.Value!;

        if (command.Email.HasValue)
            user.Email = command.Email.Value!;

        if (command.Phone.HasValue)
            user.Phone = command.Phone.Value;

        if (command.IsActive.HasValue)
        {
            if (command.IsActive.Value) user.Activate(); else user.Deactivate();
        }

        if (command.IsInternalAuthEnabled.HasValue)
            user.IsInternalAuthEnabled = command.IsInternalAuthEnabled.Value;

        if (command.TwoFactorEnabled.HasValue)
        {
            if (command.TwoFactorEnabled.Value)
            {
                var channel = command.TwoFactorChannel is { HasValue: true, Value: not null }
                    ? command.TwoFactorChannel.Value.Value
                    : TwoFactorChannel.Email;
                user.EnableTwoFactor(channel);
            }
            else
                user.DisableTwoFactor();
        }

        if (command.Locale.HasValue)
            user.Locale = command.Locale.Value!;

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
