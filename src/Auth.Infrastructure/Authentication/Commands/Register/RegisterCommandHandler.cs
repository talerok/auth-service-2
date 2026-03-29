using Auth.Application;
using Auth.Application.Auth.Commands.Register;
using Auth.Application.Messaging.Commands;
using Auth.Application.Messaging.Events;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Authentication.Commands.Register;

internal sealed class RegisterCommandHandler(
    AuthDbContext dbContext,
    IPasswordHasher passwordHasher,
    IEventBus eventBus) : IRequestHandler<RegisterCommand, UserDto>
{
    public async Task<UserDto> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Users.AnyAsync(
            x => x.Username == command.Username || x.Email == command.Email, cancellationToken);
        if (exists)
        {
            throw new AuthException(AuthErrorCatalog.DuplicateIdentity);
        }

        var user = new User
        {
            Username = command.Username,
            FullName = command.FullName,
            Email = command.Email,
            IsActive = true,
            IsInternalAuthEnabled = true
        };
        user.SetPassword(passwordHasher.Hash(command.Password));

        dbContext.Users.Add(user);
        await eventBus.PublishAsync(new UserCreatedEvent { UserId = user.Id, Username = user.Username, Email = user.Email }, cancellationToken);
        await eventBus.PublishAsync(new IndexEntityRequested { EntityType = IndexEntityType.User, EntityId = user.Id, Operation = IndexOperation.Index }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new UserDto(user.Id, user.Username, user.FullName, user.Email, user.Phone, user.IsActive, user.IsInternalAuthEnabled, user.MustChangePassword, user.TwoFactorEnabled, user.TwoFactorChannel, user.Locale, user.EmailVerified, user.PhoneVerified, user.PasswordMaxAgeDays, user.PasswordChangedAt);
        return dto;
    }
}
