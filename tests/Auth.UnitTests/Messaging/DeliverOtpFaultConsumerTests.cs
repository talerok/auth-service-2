using Auth.Application.Messaging.Commands;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Messaging.Consumers;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Messaging;

public sealed class DeliverOtpFaultConsumerTests
{
    private static Mock<ConsumeContext<Fault<DeliverOtpRequested>>> CreateFaultContext(
        Guid challengeId, string errorMessage = "Delivery failed")
    {
        var exceptionInfo = new Mock<ExceptionInfo>();
        exceptionInfo.Setup(x => x.Message).Returns(errorMessage);

        var fault = new Mock<Fault<DeliverOtpRequested>>();
        fault.Setup(x => x.Message).Returns(new DeliverOtpRequested { ChallengeId = challengeId });
        fault.Setup(x => x.Exceptions).Returns([exceptionInfo.Object]);

        var context = new Mock<ConsumeContext<Fault<DeliverOtpRequested>>>();
        context.Setup(x => x.Message).Returns(fault.Object);
        context.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
        return context;
    }

    [Fact]
    public async Task Consume_PendingChallenge_MarksDeliveryFailed()
    {
        await using var dbContext = CreateDbContext();
        var challenge = TwoFactorChallenge.Create(
            Guid.NewGuid(), TwoFactorChallenge.PurposeLogin, TwoFactorChannel.Email,
            "hash", "salt", "encrypted", DateTime.UtcNow.AddMinutes(5), 5);
        dbContext.TwoFactorChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var consumer = new DeliverOtpFaultConsumer(dbContext, NullLogger<DeliverOtpFaultConsumer>.Instance);
        var context = CreateFaultContext(challenge.Id);

        await consumer.Consume(context.Object);

        var updated = await dbContext.TwoFactorChallenges.FirstAsync(c => c.Id == challenge.Id);
        updated.DeliveryStatus.Should().Be(TwoFactorChallenge.DeliveryFailed);
    }

    [Fact]
    public async Task Consume_ChallengeNotFound_DoesNotThrow()
    {
        await using var dbContext = CreateDbContext();
        var consumer = new DeliverOtpFaultConsumer(dbContext, NullLogger<DeliverOtpFaultConsumer>.Instance);
        var context = CreateFaultContext(Guid.NewGuid());

        await consumer.Invoking(c => c.Consume(context.Object)).Should().NotThrowAsync();
    }

    [Fact]
    public async Task Consume_AlreadyDelivered_DoesNotOverwrite()
    {
        await using var dbContext = CreateDbContext();
        var challenge = TwoFactorChallenge.Create(
            Guid.NewGuid(), TwoFactorChallenge.PurposeLogin, TwoFactorChannel.Email,
            "hash", "salt", "encrypted", DateTime.UtcNow.AddMinutes(5), 5);
        challenge.MarkDelivered();
        dbContext.TwoFactorChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var consumer = new DeliverOtpFaultConsumer(dbContext, NullLogger<DeliverOtpFaultConsumer>.Instance);
        var context = CreateFaultContext(challenge.Id);

        await consumer.Consume(context.Object);

        var updated = await dbContext.TwoFactorChallenges.FirstAsync(c => c.Id == challenge.Id);
        updated.DeliveryStatus.Should().Be(TwoFactorChallenge.DeliveryDelivered);
    }
}
