using Auth.Domain;

namespace Auth.Application;

public interface IAuthService
{
    Task<User> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken);
    Task<User> GetActiveUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<User> ValidateForcedPasswordChangeAsync(ForcedPasswordChangeRequest request, CancellationToken cancellationToken);
    Task<PasswordChangeChallenge> CreatePasswordChangeChallengeAsync(Guid userId, CancellationToken cancellationToken);
    Task<TwoFactorChallenge> CreateLoginChallengeAsync(Guid userId, TwoFactorChannel channel, CancellationToken cancellationToken);
}
