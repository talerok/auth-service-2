using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure;

public sealed class SafeDefaultTwoFactorEmailGateway(
    IHostEnvironment hostEnvironment,
    ILogger<SafeDefaultTwoFactorEmailGateway> logger) : ITwoFactorEmailGateway
{
    public Task<TwoFactorDeliveryResult> SendAsync(
        Guid challengeId, string email, string subject, string body, CancellationToken cancellationToken)
    {
        if (hostEnvironment.IsDevelopment() || hostEnvironment.IsEnvironment("Testing"))
            return Task.FromResult(TwoFactorDeliveryResult.Delivered);

        logger.LogWarning(
            "Default 2FA email gateway used in non-development environment for challenge {ChallengeId}",
            challengeId);
        return Task.FromResult(TwoFactorDeliveryResult.ProviderUnavailable);
    }
}
