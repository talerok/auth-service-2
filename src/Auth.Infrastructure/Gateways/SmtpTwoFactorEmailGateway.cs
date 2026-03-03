using System.Text.RegularExpressions;
using Auth.Application;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Auth.Infrastructure;

public sealed partial class SmtpTwoFactorEmailGateway(
    IOptions<IntegrationOptions> options,
    ILogger<SmtpTwoFactorEmailGateway> logger) : ITwoFactorEmailGateway
{
    private readonly SmtpOptions _smtp = options.Value.Smtp;

    public async Task<TwoFactorDeliveryResult> SendAsync(
        Guid challengeId, string email, string subject, string body,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = BuildEmailMessage(_smtp, email, subject, body);

            using var client = new SmtpClient();

            var secureSocketOptions = _smtp.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTlsWhenAvailable;

            await client.ConnectAsync(_smtp.Host, _smtp.Port, secureSocketOptions, cancellationToken);

            if (!string.IsNullOrEmpty(_smtp.Username))
                await client.AuthenticateAsync(_smtp.Username, _smtp.Password, cancellationToken);

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            return TwoFactorDeliveryResult.Delivered;
        }
        catch (SmtpCommandException ex)
        {
            logger.LogError(ex, "SMTP send failed for challenge {ChallengeId}", challengeId);
            return TwoFactorDeliveryResult.DeliveryFailed;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SMTP send failed for challenge {ChallengeId}", challengeId);
            return TwoFactorDeliveryResult.ProviderUnavailable;
        }
    }

    internal static MimeMessage BuildEmailMessage(
        SmtpOptions options, string toEmail, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(options.FromName, options.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody = body,
            TextBody = StripHtmlTags(body)
        };

        message.Body = builder.ToMessageBody();
        return message;
    }

    internal static string StripHtmlTags(string html) =>
        string.IsNullOrWhiteSpace(html) ? html : HtmlTagRegex().Replace(html, "").Trim();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();
}
