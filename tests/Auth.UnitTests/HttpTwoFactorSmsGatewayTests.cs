using System.Net;
using System.Text.Json;
using Auth.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace Auth.UnitTests;

public sealed class HttpTwoFactorSmsGatewayTests
{
    [Fact]
    public async Task SendAsync_WhenGatewayReturns200_ReturnsDelivered()
    {
        var handler = CreateHandler(HttpStatusCode.OK, new { status = "accepted" });
        var gateway = CreateGateway(handler);

        var result = await gateway.SendAsync(Guid.NewGuid(), "+71234567890", "Your code: 123456", CancellationToken.None);

        result.Should().Be(TwoFactorDeliveryResult.Delivered);
    }

    [Fact]
    public async Task SendAsync_WhenGatewayReturns422_ReturnsDeliveryFailed()
    {
        var handler = CreateHandler((HttpStatusCode)422, new { status = "rejected", reason = "invalid_phone" });
        var gateway = CreateGateway(handler);

        var result = await gateway.SendAsync(Guid.NewGuid(), "+invalid", "Your code: 123456", CancellationToken.None);

        result.Should().Be(TwoFactorDeliveryResult.DeliveryFailed);
    }

    [Fact]
    public async Task SendAsync_WhenGatewayReturns503_ReturnsProviderUnavailable()
    {
        var handler = CreateHandler(HttpStatusCode.ServiceUnavailable, new { status = "unavailable", reason = "provider_down" });
        var gateway = CreateGateway(handler);

        var result = await gateway.SendAsync(Guid.NewGuid(), "+71234567890", "Your code: 123456", CancellationToken.None);

        result.Should().Be(TwoFactorDeliveryResult.ProviderUnavailable);
    }

    [Fact]
    public async Task SendAsync_WhenHttpRequestThrows_ReturnsProviderUnavailable()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("connection refused"));

        var gateway = CreateGateway(handler.Object);

        var result = await gateway.SendAsync(Guid.NewGuid(), "+71234567890", "Your code: 123456", CancellationToken.None);

        result.Should().Be(TwoFactorDeliveryResult.ProviderUnavailable);
    }

    [Fact]
    public async Task SendAsync_WhenTimeout_ReturnsProviderUnavailable()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("request timed out"));

        var gateway = CreateGateway(handler.Object);

        var result = await gateway.SendAsync(Guid.NewGuid(), "+71234567890", "Your code: 123456", CancellationToken.None);

        result.Should().Be(TwoFactorDeliveryResult.ProviderUnavailable);
    }

    [Fact]
    public async Task SendAsync_SendsCorrectRequestBody()
    {
        string? capturedBody = null;
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage req, CancellationToken _) =>
            {
                capturedBody = await req.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"status":"accepted"}""",
                        System.Text.Encoding.UTF8, "application/json")
                };
            });

        var challengeId = Guid.NewGuid();
        var gateway = CreateGateway(handler.Object);

        await gateway.SendAsync(challengeId, "+71234567890", "Your code: 482916", CancellationToken.None);

        capturedBody.Should().NotBeNull();
        var doc = JsonDocument.Parse(capturedBody!);
        doc.RootElement.GetProperty("requestId").GetString().Should().Be(challengeId.ToString());
        doc.RootElement.GetProperty("phone").GetString().Should().Be("+71234567890");
        doc.RootElement.GetProperty("message").GetString().Should().Be("Your code: 482916");
    }

    private static HttpTwoFactorSmsGateway CreateGateway(HttpMessageHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://sms-gateway:8080")
        };
        factory.Setup(x => x.CreateClient("SmsGateway")).Returns(client);

        return new HttpTwoFactorSmsGateway(
            factory.Object,
            NullLogger<HttpTwoFactorSmsGateway>.Instance);
    }

    private static HttpMessageHandler CreateHandler(HttpStatusCode statusCode, object body)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    System.Text.Encoding.UTF8,
                    "application/json")
            });

        return handler.Object;
    }
}
