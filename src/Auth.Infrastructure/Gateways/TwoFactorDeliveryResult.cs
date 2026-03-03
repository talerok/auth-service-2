namespace Auth.Infrastructure;

public enum TwoFactorDeliveryResult
{
    Delivered,
    DeliveryFailed,
    ProviderUnavailable
}
