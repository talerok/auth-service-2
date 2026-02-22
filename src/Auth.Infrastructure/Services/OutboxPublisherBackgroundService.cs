using Auth.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure;

public sealed class OutboxPublisherBackgroundService(
    IServiceProvider serviceProvider,
    IKafkaProducer kafkaProducer,
    ILogger<OutboxPublisherBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox publisher iteration failed");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task PublishPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var pending = await dbContext.OutboxMessages
            .Where(x => x.ProcessedAt == null)
            .OrderBy(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var message in pending)
        {
            try
            {
                await kafkaProducer.PublishAsync(message.Topic, message.Key, message.Payload, cancellationToken);
                message.ProcessedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to publish outbox message {OutboxMessageId}", message.Id);
                break;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
