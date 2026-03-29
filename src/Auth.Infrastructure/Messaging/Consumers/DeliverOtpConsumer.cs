using Auth.Application.Messaging.Commands;
using Auth.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Auth.Infrastructure.Messaging.Consumers;

internal sealed class DeliverOtpConsumer(
    AuthDbContext dbContext,
    ITwoFactorEmailGateway emailGateway,
    ITwoFactorSmsGateway smsGateway,
    IDistributedCache cache,
    IOptions<IntegrationOptions> options,
    ILogger<DeliverOtpConsumer> logger) : IConsumer<DeliverOtpRequested>
{
    private readonly VerificationOptions _verification = options.Value.Verification;
    private readonly string _encryptionKey = options.Value.EncryptionKey;

    private const string TemplatesCacheKey = "otp:notification-templates";
    private static readonly DistributedCacheEntryOptions TemplateCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public async Task Consume(ConsumeContext<DeliverOtpRequested> context)
    {
        var challenge = await dbContext.TwoFactorChallenges
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == context.Message.ChallengeId, context.CancellationToken);

        if (challenge is null)
        {
            logger.LogWarning("Challenge {ChallengeId} not found, skipping delivery", context.Message.ChallengeId);
            return;
        }

        if (challenge.DeliveryStatus != TwoFactorChallenge.DeliveryPending)
        {
            logger.LogInformation("Challenge {ChallengeId} already processed (status={Status})", challenge.Id, challenge.DeliveryStatus);
            return;
        }

        if (challenge.Channel == TwoFactorChannel.Sms && string.IsNullOrWhiteSpace(challenge.User?.Phone))
        {
            challenge.MarkDeliveryFailed();
            await dbContext.SaveChangesAsync(context.CancellationToken);
            logger.LogWarning("SMS delivery skipped — user has no phone for challenge {ChallengeId}", challenge.Id);
            return;
        }

        var templates = await GetTemplatesAsync(context.CancellationToken);
        var otp = TwoFactorOtpSecurity.DecryptOtp(challenge.OtpEncrypted, _encryptionKey);
        var locale = challenge.User?.Locale ?? "en-US";
        var templateType = ResolveTemplateType(challenge.Purpose, challenge.Channel);
        var tmpl = ResolveTemplate(templates, templateType, locale);

        if (tmpl is null)
        {
            logger.LogWarning("Template {Type}/{Locale} not found for challenge {ChallengeId}", templateType, locale, challenge.Id);
            challenge.MarkDeliveryFailed();
            await dbContext.SaveChangesAsync(context.CancellationToken);
            return;
        }

        var link = BuildVerificationLink(challenge.Purpose, challenge.Id, otp);
        var result = await DeliverAsync(challenge, otp, link, challenge.User!.Email, challenge.User.Phone, tmpl, context.CancellationToken);

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
                throw new InvalidOperationException($"OTP delivery provider unavailable for challenge {challenge.Id}");
        }

        await dbContext.SaveChangesAsync(context.CancellationToken);
    }

    private async Task<List<CachedTemplate>> GetTemplatesAsync(CancellationToken ct)
    {
        var cached = await cache.GetStringAsync(TemplatesCacheKey, ct);
        if (cached is not null)
            return JsonSerializer.Deserialize<List<CachedTemplate>>(cached) ?? [];

        var templates = await dbContext.NotificationTemplates
            .AsNoTracking()
            .Select(t => new CachedTemplate(t.Type, t.Locale, t.Subject, t.Body))
            .ToListAsync(ct);

        await cache.SetStringAsync(TemplatesCacheKey, JsonSerializer.Serialize(templates), TemplateCacheOptions, ct);
        return templates;
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

    private static CachedTemplate? ResolveTemplate(
        List<CachedTemplate> templates, NotificationTemplateType type, string locale) =>
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

    private async Task<TwoFactorDeliveryResult> DeliverAsync(
        TwoFactorChallenge challenge, string otp, string link, string email, string? phone,
        CachedTemplate template, CancellationToken ct)
    {
        var subject = RenderTemplate(template.Subject, otp, email, null, link);
        var body = RenderTemplate(template.Body, otp, email, phone, link);

        return challenge.Channel switch
        {
            TwoFactorChannel.Sms => await smsGateway.SendAsync(challenge.Id, phone!, body, ct),
            _ => await emailGateway.SendAsync(challenge.Id, email, subject, body, ct)
        };
    }

    private static string RenderTemplate(string template, string otp, string? email, string? phone, string? link) =>
        template
            .Replace("{{otp}}", otp)
            .Replace("{{email}}", email ?? "")
            .Replace("{{phone}}", phone ?? "")
            .Replace("{{link}}", link ?? "");
}

internal sealed record CachedTemplate(NotificationTemplateType Type, string Locale, string Subject, string Body);

internal sealed class DeliverOtpConsumerDefinition : ConsumerDefinition<DeliverOtpConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<DeliverOtpConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Exponential(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)));
    }
}
