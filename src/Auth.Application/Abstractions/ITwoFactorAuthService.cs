namespace Auth.Application;

public interface ITwoFactorAuthService
{
    Task<EnableTwoFactorResponse> EnableTwoFactorAsync(Guid userId, EnableTwoFactorRequest request, CancellationToken cancellationToken);
    Task ConfirmTwoFactorActivationAsync(Guid userId, VerifyTwoFactorRequest request, CancellationToken cancellationToken);
    Task DisableTwoFactorAsync(Guid userId, CancellationToken cancellationToken);
    Task<AuthTokensResponse> VerifyTwoFactorLoginAsync(VerifyTwoFactorRequest request, CancellationToken cancellationToken);
}
