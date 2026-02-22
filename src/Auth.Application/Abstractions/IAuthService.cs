namespace Auth.Application;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthTokensResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken);
    Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task RevokeAsync(RevokeRequest request, CancellationToken cancellationToken);
    Task<AuthTokensResponse> ForcedChangePasswordAsync(ForcedPasswordChangeRequest request, CancellationToken cancellationToken);
}
