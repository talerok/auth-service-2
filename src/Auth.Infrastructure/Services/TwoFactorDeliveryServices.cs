using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure;

public enum TwoFactorDeliveryResult
{
    Delivered,
    DeliveryFailed,
    ProviderUnavailable
}

public interface ITwoFactorEmailGateway
{
    Task<TwoFactorDeliveryResult> SendAsync(Guid challengeId, string email, string subject, string body, CancellationToken cancellationToken);
}

public interface ITwoFactorSmsGateway
{
    Task<TwoFactorDeliveryResult> SendAsync(Guid challengeId, string phone, string message, CancellationToken cancellationToken);
}

public sealed class SafeDefaultTwoFactorEmailGateway(
    IHostEnvironment hostEnvironment,
    ILogger<SafeDefaultTwoFactorEmailGateway> logger) : ITwoFactorEmailGateway
{
    public Task<TwoFactorDeliveryResult> SendAsync(
        Guid challengeId,
        string email,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        if (hostEnvironment.IsDevelopment() || hostEnvironment.IsEnvironment("Testing"))
        {
            return Task.FromResult(TwoFactorDeliveryResult.Delivered);
        }

        logger.LogWarning(
            "Default 2FA email gateway used in non-development environment for challenge {ChallengeId}",
            challengeId);
        return Task.FromResult(TwoFactorDeliveryResult.ProviderUnavailable);
    }
}

public sealed class SafeDefaultTwoFactorSmsGateway(
    IHostEnvironment hostEnvironment,
    ILogger<SafeDefaultTwoFactorSmsGateway> logger) : ITwoFactorSmsGateway
{
    public Task<TwoFactorDeliveryResult> SendAsync(
        Guid challengeId,
        string phone,
        string message,
        CancellationToken cancellationToken)
    {
        if (hostEnvironment.IsDevelopment() || hostEnvironment.IsEnvironment("Testing"))
        {
            return Task.FromResult(TwoFactorDeliveryResult.Delivered);
        }

        logger.LogWarning(
            "Default 2FA SMS gateway used in non-development environment for challenge {ChallengeId}",
            challengeId);
        return Task.FromResult(TwoFactorDeliveryResult.ProviderUnavailable);
    }
}

public sealed class TwoFactorDeliveryBackgroundService(
    IServiceProvider serviceProvider,
    ITwoFactorEmailGateway emailGateway,
    ITwoFactorSmsGateway smsGateway,
    IOptions<IntegrationOptions> options,
    ILogger<TwoFactorDeliveryBackgroundService> logger) : BackgroundService
{
    private readonly TwoFactorOptions _twoFactor = options.Value.TwoFactor;
    private readonly string _twoFactorKeyMaterial = string.IsNullOrWhiteSpace(options.Value.TwoFactor.EncryptionKey)
        ? options.Value.Jwt.Secret
        : options.Value.TwoFactor.EncryptionKey;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "2FA delivery worker iteration failed");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(_twoFactor.DeliveryPollIntervalMilliseconds), stoppingToken);
        }
    }

    private async Task ProcessPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var pending = await dbContext.TwoFactorChallenges
            .Where(x => x.DeliveryStatus == TwoFactorChallenge.DeliveryPending)
            .OrderBy(x => x.CreatedAt)
            .Take(20)
            .Join(
                dbContext.Users,
                challenge => challenge.UserId,
                user => user.Id,
                (challenge, user) => new
                {
                    Challenge = challenge,
                    user.Email,
                    user.Phone
                })
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        var templates = await dbContext.NotificationTemplates
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Channel, cancellationToken);

        foreach (var item in pending)
        {
            if (item.Challenge.Channel == TwoFactorChannel.Sms && string.IsNullOrWhiteSpace(item.Phone))
            {
                item.Challenge.MarkDeliveryFailed();
                logger.LogWarning("SMS 2FA delivery skipped — user has no phone for challenge {ChallengeId}", item.Challenge.Id);
                continue;
            }

            var attemptsLeft = _twoFactor.DeliveryRetryCount;
            TwoFactorDeliveryResult? finalResult = null;
            while (attemptsLeft-- > 0)
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(_twoFactor.DeliveryTimeoutSeconds));
                try
                {
                    var otp = TwoFactorOtpSecurity.DecryptOtp(item.Challenge.OtpEncrypted, _twoFactorKeyMaterial);
                    finalResult = item.Challenge.Channel switch
                    {
                        TwoFactorChannel.Sms => await SendSmsAsync(item.Challenge.Id, item.Phone!, otp, templates, timeout.Token),
                        _ => await SendEmailAsync(item.Challenge.Id, item.Email, otp, templates, timeout.Token)
                    };
                    if (finalResult == TwoFactorDeliveryResult.ProviderUnavailable && attemptsLeft > 0)
                    {
                        await Task.Delay(_twoFactor.DeliveryRetryBackoffMilliseconds, cancellationToken);
                        continue;
                    }

                    break;
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    finalResult = TwoFactorDeliveryResult.ProviderUnavailable;
                    if (attemptsLeft > 0)
                    {
                        await Task.Delay(_twoFactor.DeliveryRetryBackoffMilliseconds, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "2FA delivery provider unavailable for challenge {ChallengeId}", item.Challenge.Id);
                    finalResult = TwoFactorDeliveryResult.ProviderUnavailable;
                    if (attemptsLeft > 0)
                    {
                        await Task.Delay(_twoFactor.DeliveryRetryBackoffMilliseconds, cancellationToken);
                    }
                }
            }

            switch (finalResult)
            {
                case TwoFactorDeliveryResult.Delivered:
                    item.Challenge.MarkDelivered();
                    break;
                case TwoFactorDeliveryResult.DeliveryFailed:
                    item.Challenge.MarkDeliveryFailed();
                    break;
                default:
                    item.Challenge.MarkProviderUnavailable();
                    break;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<TwoFactorDeliveryResult> SendEmailAsync(
        Guid challengeId, string email, string otp,
        Dictionary<TwoFactorChannel, NotificationTemplate> templates,
        CancellationToken cancellationToken)
    {
        if (!templates.TryGetValue(TwoFactorChannel.Email, out var template))
        {
            logger.LogWarning("Email notification template not found for challenge {ChallengeId}", challengeId);
            return TwoFactorDeliveryResult.DeliveryFailed;
        }

        var subject = RenderTemplate(template.Subject, otp, email, null);
        var body = RenderTemplate(template.Body, otp, email, null);
        return await emailGateway.SendAsync(challengeId, email, subject, body, cancellationToken);
    }

    private async Task<TwoFactorDeliveryResult> SendSmsAsync(
        Guid challengeId, string phone, string otp,
        Dictionary<TwoFactorChannel, NotificationTemplate> templates,
        CancellationToken cancellationToken)
    {
        if (!templates.TryGetValue(TwoFactorChannel.Sms, out var template))
        {
            logger.LogWarning("SMS notification template not found for challenge {ChallengeId}", challengeId);
            return TwoFactorDeliveryResult.DeliveryFailed;
        }

        var message = RenderTemplate(template.Body, otp, null, phone);
        return await smsGateway.SendAsync(challengeId, phone, message, cancellationToken);
    }

    private static string RenderTemplate(string template, string otp, string? email, string? phone) =>
        template
            .Replace("{{otp}}", otp)
            .Replace("{{email}}", email ?? "")
            .Replace("{{phone}}", phone ?? "");
}
