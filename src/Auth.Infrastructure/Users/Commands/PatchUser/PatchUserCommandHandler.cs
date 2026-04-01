using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Application.Messaging.Events;
using Auth.Application.Users.Commands.PatchUser;
using Auth.Domain;
using Auth.Infrastructure.AuditLogs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Users.Commands.PatchUser;

internal sealed class PatchUserCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    IAuditContext auditContext) : IRequestHandler<PatchUserCommand, UserDto?>
{
    public async Task<UserDto?> Handle(PatchUserCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (user is null)
            return null;

        ApplyPatchFields(user, command);

        var changes = AuditDiff.CaptureChanges(dbContext.Entry(user));
        if (changes.Count > 0)
            auditContext.Details = changes;

        await eventBus.PublishAsync(new UserUpdatedEvent { UserId = user.Id, ChangedFields = changes.Keys.ToArray() }, cancellationToken);
        await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.User, EntityId = user.Id, Operation = IndexOperation.Index }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new UserDto(user.Id, user.Username, user.FullName, user.Email, user.Phone,
            user.IsActive, user.IsInternalAuthEnabled, user.MustChangePassword, user.TwoFactorEnabled, user.TwoFactorChannel,
            user.Locale, user.EmailVerified, user.PhoneVerified, user.PasswordMaxAgeDays, user.PasswordChangedAt);
    }

    private static void ApplyPatchFields(User user, PatchUserCommand command)
    {
        ApplyProfileFields(user, command);
        ApplySecurityFields(user, command);
    }

    private static void ApplyProfileFields(User user, PatchUserCommand command)
    {
        if (command.Username.HasValue)
            user.Username = command.Username.Value!;

        if (command.FullName.HasValue)
            user.FullName = command.FullName.Value!;

        if (command.Email.HasValue)
            user.Email = command.Email.Value!;

        if (command.Phone.HasValue)
            user.Phone = command.Phone.Value;

        if (command.Locale.HasValue)
            user.Locale = command.Locale.Value!;

        if (command.EmailVerified.HasValue)
            user.EmailVerified = command.EmailVerified.Value;

        if (command.PhoneVerified.HasValue)
            user.PhoneVerified = command.PhoneVerified.Value;
    }

    private static void ApplySecurityFields(User user, PatchUserCommand command)
    {
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

        if (command.PasswordMaxAgeDays.HasValue)
            user.PasswordMaxAgeDays = command.PasswordMaxAgeDays.Value;
    }
}
