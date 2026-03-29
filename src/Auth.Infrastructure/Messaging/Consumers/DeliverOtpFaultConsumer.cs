using Auth.Application.Messaging.Commands;
using Auth.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.Messaging.Consumers;

internal sealed class DeliverOtpFaultConsumer(
    AuthDbContext dbContext,
    ILogger<DeliverOtpFaultConsumer> logger) : IConsumer<Fault<DeliverOtpRequested>>
{
    public async Task Consume(ConsumeContext<Fault<DeliverOtpRequested>> context)
    {
        var challengeId = context.Message.Message.ChallengeId;

        var challenge = await dbContext.TwoFactorChallenges
            .FirstOrDefaultAsync(c => c.Id == challengeId, context.CancellationToken);

        if (challenge is not null && challenge.DeliveryStatus == TwoFactorChallenge.DeliveryPending)
        {
            challenge.MarkDeliveryFailed();
            await dbContext.SaveChangesAsync(context.CancellationToken);
        }

        var errors = string.Join("; ", context.Message.Exceptions.Select(e => e.Message));
        logger.LogError("OTP delivery permanently failed for challenge {ChallengeId}: {Errors}", challengeId, errors);
    }
}
