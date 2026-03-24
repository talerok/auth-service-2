using Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure;

public sealed class TwoFactorDeliveryBackgroundService(
    IServiceProvider serviceProvider,
    ITwoFactorEmailGateway emailGateway,
    ITwoFactorSmsGateway smsGateway,
    IOptions<IntegrationOptions> options,
    ILogger<TwoFactorDeliveryBackgroundService> logger) : BackgroundService
{
    private readonly TwoFactorOptions _twoFactor = options.Value.TwoFactor;
    private readonly string _twoFactorKeyMaterial = options.Value.EncryptionKey;

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
            .Include(c => c.User)
            .Where(c => c.DeliveryStatus == TwoFactorChallenge.DeliveryPending)
            .OrderBy(c => c.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
            return;

        var templates = await dbContext.NotificationTemplates
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Channel, cancellationToken);

        foreach (var challenge in pending)
        {
            if (challenge.Channel == TwoFactorChannel.Sms && string.IsNullOrWhiteSpace(challenge.User?.Phone))
            {
                challenge.MarkDeliveryFailed();
                logger.LogWarning("SMS 2FA delivery skipped — user has no phone for challenge {ChallengeId}", challenge.Id);
                continue;
            }

            var otp = TwoFactorOtpSecurity.DecryptOtp(challenge.OtpEncrypted, _twoFactorKeyMaterial);
            var result = await DeliverWithRetryAsync(challenge, otp, challenge.User!.Email, challenge.User.Phone, templates, cancellationToken);

            switch (result)
            {
                case TwoFactorDeliveryResult.Delivered:
                    challenge.MarkDelivered();
                    break;
                case TwoFactorDeliveryResult.DeliveryFailed:
                    challenge.MarkDeliveryFailed();
                    break;
                default:
                    challenge.MarkProviderUnavailable();
                    break;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<TwoFactorDeliveryResult> DeliverWithRetryAsync(
        TwoFactorChallenge challenge, string otp, string email, string? phone,
        Dictionary<TwoFactorChannel, NotificationTemplate> templates,
        CancellationToken cancellationToken)
    {
        var result = TwoFactorDeliveryResult.ProviderUnavailable;

        for (var attempt = 0; attempt < _twoFactor.DeliveryRetryCount; attempt++)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(_twoFactor.DeliveryTimeoutSeconds));

            try
            {
                result = challenge.Channel switch
                {
                    TwoFactorChannel.Sms => await SendSmsAsync(challenge.Id, phone!, otp, templates, timeout.Token),
                    _ => await SendEmailAsync(challenge.Id, email, otp, templates, timeout.Token)
                };

                if (result != TwoFactorDeliveryResult.ProviderUnavailable)
                    return result;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                result = TwoFactorDeliveryResult.ProviderUnavailable;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "2FA delivery provider unavailable for challenge {ChallengeId}", challenge.Id);
                result = TwoFactorDeliveryResult.ProviderUnavailable;
            }

            if (attempt + 1 < _twoFactor.DeliveryRetryCount)
                await Task.Delay(_twoFactor.DeliveryRetryBackoffMilliseconds, cancellationToken);
        }

        return result;
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
