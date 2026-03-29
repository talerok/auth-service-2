using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Application.Messaging.Events;
using Auth.Application.Users.Commands.UpdateUser;
using Auth.Domain;
using Auth.Infrastructure.AuditLogs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Users.Commands.UpdateUser;

internal sealed class UpdateUserCommandHandler(
    AuthDbContext dbContext,
    IEventBus eventBus,
    IAuditContext auditContext) : IRequestHandler<UpdateUserCommand, UserDto?>
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

        user.Locale = command.Locale;
        user.EmailVerified = command.EmailVerified;
        user.PhoneVerified = command.PhoneVerified;
        user.PasswordMaxAgeDays = command.PasswordMaxAgeDays;

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
}
