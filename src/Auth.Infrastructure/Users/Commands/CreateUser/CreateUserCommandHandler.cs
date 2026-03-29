using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Application.Messaging.Events;
using Auth.Application.Users.Commands.CreateUser;
using Auth.Domain;
using Auth.Infrastructure.AuditLogs;
using MediatR;

namespace Auth.Infrastructure.Users.Commands.CreateUser;

internal sealed class CreateUserCommandHandler(
    AuthDbContext dbContext,
    IPasswordHasher passwordHasher,
    IEventBus eventBus,
    IAuditContext auditContext) : IRequestHandler<CreateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Id = command.EntityId,
            Username = command.Username,
            FullName = command.FullName,
            Email = command.Email,
            Phone = command.Phone,
            IsActive = command.IsActive,
            IsInternalAuthEnabled = command.IsInternalAuthEnabled,
            Locale = command.Locale,
            EmailVerified = command.EmailVerified,
            PhoneVerified = command.PhoneVerified,
            PasswordMaxAgeDays = command.PasswordMaxAgeDays
        };
        user.SetPassword(passwordHasher.Hash(command.Password));

        if (command.MustChangePassword)
            user.MarkMustChangePassword();

        if (command.TwoFactorEnabled)
            user.EnableTwoFactor(command.TwoFactorChannel ?? TwoFactorChannel.Email);

        dbContext.Users.Add(user);
        auditContext.Details = AuditDiff.CaptureState(dbContext.Entry(user));

        await eventBus.PublishAsync(new UserCreatedEvent { UserId = user.Id, Username = user.Username, Email = user.Email }, cancellationToken);
        await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.User, EntityId = user.Id, Operation = IndexOperation.Index }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new UserDto(user.Id, user.Username, user.FullName, user.Email, user.Phone,
            user.IsActive, user.IsInternalAuthEnabled, user.MustChangePassword, user.TwoFactorEnabled, user.TwoFactorChannel,
            user.Locale, user.EmailVerified, user.PhoneVerified, user.PasswordMaxAgeDays, user.PasswordChangedAt);
    }
}
