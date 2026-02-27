using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure;

public sealed class HttpTwoFactorSmsGateway(
    IHttpClientFactory httpClientFactory,
    ILogger<HttpTwoFactorSmsGateway> logger) : ITwoFactorSmsGateway
{
    public async Task<TwoFactorDeliveryResult> SendOtpAsync(
        Guid challengeId, string phone, string otp, CancellationToken cancellationToken)
    {
        try
        {
            using var client = httpClientFactory.CreateClient("SmsGateway");
            var request = new SmsGatewaySendRequest(challengeId.ToString(), phone, $"Your code: {otp}");
            var response = await client.PostAsJsonAsync("/api/sms/send", request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return TwoFactorDeliveryResult.Delivered;
            }

            if ((int)response.StatusCode == 422)
            {
                var body = await response.Content.ReadFromJsonAsync<SmsGatewayResponse>(cancellationToken);
                logger.LogWarning(
                    "SMS gateway rejected delivery for challenge {ChallengeId}, reason: {Reason}",
                    challengeId, body?.Reason);
                return TwoFactorDeliveryResult.DeliveryFailed;
            }

            logger.LogWarning(
                "SMS gateway returned {StatusCode} for challenge {ChallengeId}",
                (int)response.StatusCode, challengeId);
            return TwoFactorDeliveryResult.ProviderUnavailable;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "SMS gateway unavailable for challenge {ChallengeId}", challengeId);
            return TwoFactorDeliveryResult.ProviderUnavailable;
        }
    }

    internal sealed record SmsGatewaySendRequest(
        [property: JsonPropertyName("requestId")] string RequestId,
        [property: JsonPropertyName("phone")] string Phone,
        [property: JsonPropertyName("message")] string Message);

    internal sealed record SmsGatewayResponse(
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("reason")] string? Reason);
}
