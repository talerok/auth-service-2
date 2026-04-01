using Auth.Application;
using Auth.Infrastructure.DistributedJobs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Auth.UnitTests.DistributedJobs;

public sealed class DistributedJobBackgroundServiceTests
{
    private readonly Mock<IDistributedJob> _job = new();
    private readonly Mock<IDistributedLock> _distributedLock = new();

    private DistributedJobBackgroundService<IDistributedJob> CreateService(IServiceProvider? serviceProvider = null)
    {
        if (serviceProvider is null)
        {
            var services = new ServiceCollection();
            services.AddScoped(_ => _job.Object);
            serviceProvider = services.BuildServiceProvider();
        }

        return new DistributedJobBackgroundService<IDistributedJob>(
            serviceProvider,
            _distributedLock.Object,
            NullLogger<DistributedJobBackgroundService<IDistributedJob>>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_IntervalZero_StopsImmediately()
    {
        _job.Setup(j => j.Interval).Returns(TimeSpan.Zero);

        var service = CreateService();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await service.StartAsync(cts.Token);
        await Task.Delay(100);
        await service.StopAsync(CancellationToken.None);

        _distributedLock.Verify(
            l => l.TryAcquireAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_LockNotAcquired_SkipsExecution()
    {
        _job.Setup(j => j.Interval).Returns(TimeSpan.FromMilliseconds(50));
        _job.Setup(j => j.LockResource).Returns("job:test");
        _job.Setup(j => j.MaxBatchIterations).Returns(100);
        _distributedLock
            .Setup(l => l.TryAcquireAsync("job:test", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IAsyncDisposable?)null);

        var service = CreateService();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        await service.StartAsync(cts.Token);
        try { await Task.Delay(300, cts.Token); } catch (OperationCanceledException) { }
        await service.StopAsync(CancellationToken.None);

        _job.Verify(j => j.ExecuteAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_LockAcquired_ExecutesJobInBatchLoop()
    {
        var callCount = 0;
        _job.Setup(j => j.Interval).Returns(TimeSpan.FromMilliseconds(50));
        _job.Setup(j => j.LockResource).Returns("job:test");
        _job.Setup(j => j.MaxBatchIterations).Returns(100);
        _job.Setup(j => j.ExecuteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => ++callCount <= 2 ? 100 : 0);

        var handle = new Mock<IAsyncDisposable>();
        _distributedLock
            .Setup(l => l.TryAcquireAsync("job:test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(handle.Object);

        var service = CreateService();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        await service.StartAsync(cts.Token);
        try { await Task.Delay(300, cts.Token); } catch (OperationCanceledException) { }
        await service.StopAsync(CancellationToken.None);

        callCount.Should().BeGreaterThanOrEqualTo(3);
        handle.Verify(h => h.DisposeAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_JobThrows_ContinuesNextIteration()
    {
        var callCount = 0;
        _job.Setup(j => j.Interval).Returns(TimeSpan.FromMilliseconds(50));
        _job.Setup(j => j.LockResource).Returns("job:test");
        _job.Setup(j => j.MaxBatchIterations).Returns(100);
        _job.Setup(j => j.ExecuteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1) throw new InvalidOperationException("DB error");
                return 0;
            });

        var handle = new Mock<IAsyncDisposable>();
        _distributedLock
            .Setup(l => l.TryAcquireAsync("job:test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(handle.Object);

        var service = CreateService();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        await service.StartAsync(cts.Token);
        try { await Task.Delay(400, cts.Token); } catch (OperationCanceledException) { }
        await service.StopAsync(CancellationToken.None);

        callCount.Should().BeGreaterThanOrEqualTo(2);
    }
}
