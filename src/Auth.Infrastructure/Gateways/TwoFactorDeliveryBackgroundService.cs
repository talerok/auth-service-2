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
    private readonly VerificationOptions _verification = options.Value.Verification;
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
            .ToListAsync(cancellationToken);

        foreach (var challenge in pending)
        {
            if (challenge.Channel == TwoFactorChannel.Sms && string.IsNullOrWhiteSpace(challenge.User?.Phone))
            {
                challenge.MarkDeliveryFailed();
                logger.LogWarning("SMS delivery skipped — user has no phone for challenge {ChallengeId}", challenge.Id);
                continue;
            }

            var otp = TwoFactorOtpSecurity.DecryptOtp(challenge.OtpEncrypted, _twoFactorKeyMaterial);
            var locale = challenge.User?.Locale ?? "en-US";
            var templateType = ResolveTemplateType(challenge.Purpose, challenge.Channel);
            var template = ResolveTemplate(templates, templateType, locale);

            if (template is null)
            {
                logger.LogWarning("Template {Type}/{Locale} not found for challenge {ChallengeId}", templateType, locale, challenge.Id);
                challenge.MarkDeliveryFailed();
                continue;
            }

            var link = BuildVerificationLink(challenge.Purpose, challenge.Id, otp);
            var result = await DeliverWithRetryAsync(challenge, otp, link, challenge.User!.Email, challenge.User.Phone, template, cancellationToken);

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

    private static NotificationTemplateType ResolveTemplateType(string purpose, TwoFactorChannel channel) =>
        purpose switch
        {
            TwoFactorChallenge.PurposeEmailVerification => NotificationTemplateType.EmailVerification,
            TwoFactorChallenge.PurposePhoneVerification => NotificationTemplateType.PhoneVerification,
            _ => channel == TwoFactorChannel.Sms
                ? NotificationTemplateType.TwoFactorSms
                : NotificationTemplateType.TwoFactorEmail
        };

    private static NotificationTemplate? ResolveTemplate(
        List<NotificationTemplate> templates, NotificationTemplateType type, string locale) =>
        templates.FirstOrDefault(t => t.Type == type && t.Locale.Equals(locale, StringComparison.OrdinalIgnoreCase))
        ?? templates.FirstOrDefault(t => t.Type == type && t.Locale.Equals("en-US", StringComparison.OrdinalIgnoreCase));

    private string BuildVerificationLink(string purpose, Guid challengeId, string otp)
    {
        var baseUrl = purpose switch
        {
            TwoFactorChallenge.PurposeEmailVerification => _verification.EmailBaseUrl,
            TwoFactorChallenge.PurposePhoneVerification => _verification.PhoneBaseUrl,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(baseUrl))
            return string.Empty;

        var separator = baseUrl.Contains('?') ? "&" : "?";
        return $"{baseUrl}{separator}challengeId={challengeId}&code={Uri.EscapeDataString(otp)}";
    }

    private async Task<TwoFactorDeliveryResult> DeliverWithRetryAsync(
        TwoFactorChallenge challenge, string otp, string link, string email, string? phone,
        NotificationTemplate template,
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
                    TwoFactorChannel.Sms => await SendSmsAsync(challenge.Id, phone!, otp, link, template, timeout.Token),
                    _ => await SendEmailAsync(challenge.Id, email, otp, link, template, timeout.Token)
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
                logger.LogWarning(ex, "Delivery provider unavailable for challenge {ChallengeId}", challenge.Id);
                result = TwoFactorDeliveryResult.ProviderUnavailable;
            }

            if (attempt + 1 < _twoFactor.DeliveryRetryCount)
                await Task.Delay(_twoFactor.DeliveryRetryBackoffMilliseconds, cancellationToken);
        }

        return result;
    }

    private async Task<TwoFactorDeliveryResult> SendEmailAsync(
        Guid challengeId, string email, string otp, string link,
        NotificationTemplate template,
        CancellationToken cancellationToken)
    {
        var subject = RenderTemplate(template.Subject, otp, email, null, link);
        var body = RenderTemplate(template.Body, otp, email, null, link);
        return await emailGateway.SendAsync(challengeId, email, subject, body, cancellationToken);
    }

    private async Task<TwoFactorDeliveryResult> SendSmsAsync(
        Guid challengeId, string phone, string otp, string link,
        NotificationTemplate template,
        CancellationToken cancellationToken)
    {
        var message = RenderTemplate(template.Body, otp, null, phone, link);
        return await smsGateway.SendAsync(challengeId, phone, message, cancellationToken);
    }

    private static string RenderTemplate(string template, string otp, string? email, string? phone, string? link) =>
        template
            .Replace("{{otp}}", otp)
            .Replace("{{email}}", email ?? "")
            .Replace("{{phone}}", phone ?? "")
            .Replace("{{link}}", link ?? "");
}
