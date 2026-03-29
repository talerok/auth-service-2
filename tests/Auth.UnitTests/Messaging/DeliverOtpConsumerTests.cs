using Auth.Application.Messaging.Commands;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Messaging.Consumers;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Messaging;

public sealed class DeliverOtpConsumerTests
{
    private readonly Mock<ITwoFactorEmailGateway> _emailGateway = new();
    private readonly Mock<ITwoFactorSmsGateway> _smsGateway = new();
    private readonly Mock<IDistributedCache> _cache = new();

    public DeliverOtpConsumerTests()
    {
        _cache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
    }

    private static readonly string EncryptionKey = "test-encryption-key-for-unit-tests";

    private DeliverOtpConsumer CreateConsumer(AuthDbContext dbContext) =>
        new(dbContext, _emailGateway.Object, _smsGateway.Object, _cache.Object,
            Options.Create(new IntegrationOptions { EncryptionKey = EncryptionKey }),
            NullLogger<DeliverOtpConsumer>.Instance);

    private static Mock<ConsumeContext<DeliverOtpRequested>> CreateContext(Guid challengeId)
    {
        var context = new Mock<ConsumeContext<DeliverOtpRequested>>();
        context.Setup(x => x.Message).Returns(new DeliverOtpRequested { ChallengeId = challengeId });
        context.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
        return context;
    }

    [Fact]
    public async Task Consume_ChallengeNotFound_Skips()
    {
        await using var dbContext = CreateDbContext();
        var consumer = CreateConsumer(dbContext);
        var context = CreateContext(Guid.NewGuid());

        await consumer.Consume(context.Object);

        _emailGateway.Verify(x => x.SendAsync(It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consume_AlreadyProcessed_Skips()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "bob", Email = "bob@test.com", IsActive = true };
        dbContext.Users.Add(user);
        var challenge = TwoFactorChallenge.Create(
            user.Id, TwoFactorChallenge.PurposeLogin, TwoFactorChannel.Email,
            "hash", "salt", TwoFactorOtpSecurity.EncryptOtp("123456", EncryptionKey),
            DateTime.UtcNow.AddMinutes(5), 5);
        challenge.MarkDelivered();
        dbContext.TwoFactorChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var consumer = CreateConsumer(dbContext);
        var context = CreateContext(challenge.Id);

        await consumer.Consume(context.Object);

        _emailGateway.Verify(x => x.SendAsync(It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consume_SmsNoPhone_MarksDeliveryFailed()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "bob", Email = "bob@test.com", IsActive = true };
        dbContext.Users.Add(user);
        var challenge = TwoFactorChallenge.Create(
            user.Id, TwoFactorChallenge.PurposeLogin, TwoFactorChannel.Sms,
            "hash", "salt", TwoFactorOtpSecurity.EncryptOtp("123456", EncryptionKey),
            DateTime.UtcNow.AddMinutes(5), 5);
        dbContext.TwoFactorChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var consumer = CreateConsumer(dbContext);
        var context = CreateContext(challenge.Id);

        await consumer.Consume(context.Object);

        var updated = await dbContext.TwoFactorChallenges.FirstAsync(c => c.Id == challenge.Id);
        updated.DeliveryStatus.Should().Be(TwoFactorChallenge.DeliveryFailed);
    }

    [Fact]
    public async Task Consume_TemplateNotFound_MarksDeliveryFailed()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "bob", Email = "bob@test.com", IsActive = true };
        dbContext.Users.Add(user);
        var challenge = TwoFactorChallenge.Create(
            user.Id, TwoFactorChallenge.PurposeLogin, TwoFactorChannel.Email,
            "hash", "salt", TwoFactorOtpSecurity.EncryptOtp("123456", EncryptionKey),
            DateTime.UtcNow.AddMinutes(5), 5);
        dbContext.TwoFactorChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var consumer = CreateConsumer(dbContext);
        var context = CreateContext(challenge.Id);

        await consumer.Consume(context.Object);

        var updated = await dbContext.TwoFactorChallenges.FirstAsync(c => c.Id == challenge.Id);
        updated.DeliveryStatus.Should().Be(TwoFactorChallenge.DeliveryFailed);
    }

    [Fact]
    public async Task Consume_EmailDelivered_MarksDelivered()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "bob", Email = "bob@test.com", IsActive = true };
        dbContext.Users.Add(user);
        dbContext.NotificationTemplates.Add(new NotificationTemplate
        {
            Type = NotificationTemplateType.TwoFactorEmail, Locale = "en-US",
            Subject = "Your code: {{otp}}", Body = "Code is {{otp}}"
        });
        var challenge = TwoFactorChallenge.Create(
            user.Id, TwoFactorChallenge.PurposeLogin, TwoFactorChannel.Email,
            "hash", "salt", TwoFactorOtpSecurity.EncryptOtp("123456", EncryptionKey),
            DateTime.UtcNow.AddMinutes(5), 5);
        dbContext.TwoFactorChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        _emailGateway.Setup(x => x.SendAsync(It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TwoFactorDeliveryResult.Delivered);

        var consumer = CreateConsumer(dbContext);
        var context = CreateContext(challenge.Id);

        await consumer.Consume(context.Object);

        var updated = await dbContext.TwoFactorChallenges.FirstAsync(c => c.Id == challenge.Id);
        updated.DeliveryStatus.Should().Be(TwoFactorChallenge.DeliveryDelivered);
        _emailGateway.Verify(x => x.SendAsync(challenge.Id, "bob@test.com",
            It.Is<string>(s => s.Contains("123456")),
            It.Is<string>(s => s.Contains("123456")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_DeliveryFailed_MarksDeliveryFailed()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "bob", Email = "bob@test.com", IsActive = true };
        dbContext.Users.Add(user);
        dbContext.NotificationTemplates.Add(new NotificationTemplate
        {
            Type = NotificationTemplateType.TwoFactorEmail, Locale = "en-US",
            Subject = "Code", Body = "{{otp}}"
        });
        var challenge = TwoFactorChallenge.Create(
            user.Id, TwoFactorChallenge.PurposeLogin, TwoFactorChannel.Email,
            "hash", "salt", TwoFactorOtpSecurity.EncryptOtp("123456", EncryptionKey),
            DateTime.UtcNow.AddMinutes(5), 5);
        dbContext.TwoFactorChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        _emailGateway.Setup(x => x.SendAsync(It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TwoFactorDeliveryResult.DeliveryFailed);

        var consumer = CreateConsumer(dbContext);
        var context = CreateContext(challenge.Id);

        await consumer.Consume(context.Object);

        var updated = await dbContext.TwoFactorChallenges.FirstAsync(c => c.Id == challenge.Id);
        updated.DeliveryStatus.Should().Be(TwoFactorChallenge.DeliveryFailed);
    }

    [Fact]
    public async Task Consume_ProviderUnavailable_ThrowsForRetry()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "bob", Email = "bob@test.com", IsActive = true };
        dbContext.Users.Add(user);
        dbContext.NotificationTemplates.Add(new NotificationTemplate
        {
            Type = NotificationTemplateType.TwoFactorEmail, Locale = "en-US",
            Subject = "Code", Body = "{{otp}}"
        });
        var challenge = TwoFactorChallenge.Create(
            user.Id, TwoFactorChallenge.PurposeLogin, TwoFactorChannel.Email,
            "hash", "salt", TwoFactorOtpSecurity.EncryptOtp("123456", EncryptionKey),
            DateTime.UtcNow.AddMinutes(5), 5);
        dbContext.TwoFactorChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        _emailGateway.Setup(x => x.SendAsync(It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TwoFactorDeliveryResult.ProviderUnavailable);

        var consumer = CreateConsumer(dbContext);
        var context = CreateContext(challenge.Id);

        await consumer.Invoking(c => c.Consume(context.Object))
            .Should().ThrowAsync<InvalidOperationException>();
    }
}
