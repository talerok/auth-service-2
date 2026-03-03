using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure;

public sealed class SafeDefaultTwoFactorSmsGateway(
    IHostEnvironment hostEnvironment,
    ILogger<SafeDefaultTwoFactorSmsGateway> logger) : ITwoFactorSmsGateway
{
    public Task<TwoFactorDeliveryResult> SendAsync(
        Guid challengeId, string phone, string message, CancellationToken cancellationToken)
    {
        if (hostEnvironment.IsDevelopment() || hostEnvironment.IsEnvironment("Testing"))
            return Task.FromResult(TwoFactorDeliveryResult.Delivered);

        logger.LogWarning(
            "Default 2FA SMS gateway used in non-development environment for challenge {ChallengeId}",
            challengeId);
        return Task.FromResult(TwoFactorDeliveryResult.ProviderUnavailable);
    }
}
