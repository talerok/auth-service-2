using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.Integration.Search;

public sealed class OpenSearchRetryExecutor(ILogger<OpenSearchRetryExecutor> logger)
{
    public async Task ExecuteAsync(Func<Task> action, string operationDescription, CancellationToken cancellationToken)
    {
        await ExecuteAsync(
            async () =>
            {
                await action();
                return true;
            },
            operationDescription,
            _ => throw new InvalidOperationException($"OpenSearch operation failed after retries: {operationDescription}"),
            cancellationToken);
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> action,
        string operationDescription,
        Func<Exception, TResult> onFinalFailure,
        CancellationToken cancellationToken)
    {
        var retries = 3;
        var delayMs = 100;
        Exception? lastError = null;

        for (var attempt = 1; attempt <= retries; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (attempt < retries)
            {
                lastError = ex;
                logger.LogWarning(
                    ex,
                    "OpenSearch operation failed, retrying: {Operation}. Attempt {Attempt}/{Retries}",
                    operationDescription,
                    attempt,
                    retries);
                await Task.Delay(delayMs, cancellationToken);
                delayMs *= 2;
            }
            catch (Exception ex)
            {
                lastError = ex;
                break;
            }
        }

        return onFinalFailure(lastError ?? new InvalidOperationException("OpenSearch operation failed"));
    }
}
