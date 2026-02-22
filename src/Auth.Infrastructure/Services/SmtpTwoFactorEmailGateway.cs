using Auth.Application;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Auth.Infrastructure;

public sealed class SmtpTwoFactorEmailGateway(
    IOptions<IntegrationOptions> options,
    ILogger<SmtpTwoFactorEmailGateway> logger) : ITwoFactorEmailGateway
{
    private readonly SmtpOptions _smtp = options.Value.Smtp;

    public async Task<TwoFactorDeliveryResult> SendOtpAsync(
        Guid challengeId, string email, string otp, CancellationToken cancellationToken)
    {
        try
        {
            var message = BuildOtpEmailMessage(_smtp, email, otp);

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

    internal static MimeMessage BuildOtpEmailMessage(SmtpOptions options, string toEmail, string otp)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(options.FromName, options.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Your verification code";

        var builder = new BodyBuilder
        {
            HtmlBody = $"""
                <html>
                <body style="font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;">
                  <div style="max-width: 480px; margin: 0 auto; background: #fff; border-radius: 8px; padding: 32px;">
                    <h2 style="color: #333;">Your verification code</h2>
                    <p style="color: #555;">Use the code below to complete your sign-in.</p>
                    <div style="font-size: 36px; font-weight: bold; letter-spacing: 8px; color: #111; margin: 24px 0;">{otp}</div>
                    <p style="color: #888; font-size: 13px;">This code expires in a few minutes. Do not share it with anyone.</p>
                    <p style="color: #ccc; font-size: 11px;">Reference: {toEmail}</p>
                  </div>
                </body>
                </html>
                """,
            TextBody = $"Your verification code: {otp}\n\nThis code expires in a few minutes. Do not share it with anyone."
        };

        message.Body = builder.ToMessageBody();
        return message;
    }
}
