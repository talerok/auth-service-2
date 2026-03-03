namespace Auth.Infrastructure;

public interface ITwoFactorEmailGateway
{
    Task<TwoFactorDeliveryResult> SendAsync(Guid challengeId, string email, string subject, string body, CancellationToken cancellationToken);
}
