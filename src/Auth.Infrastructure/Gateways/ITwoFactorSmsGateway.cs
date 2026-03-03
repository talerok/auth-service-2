namespace Auth.Infrastructure;

public interface ITwoFactorSmsGateway
{
    Task<TwoFactorDeliveryResult> SendAsync(Guid challengeId, string phone, string message, CancellationToken cancellationToken);
}
