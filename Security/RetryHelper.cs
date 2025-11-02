using Polly;
using Polly.Retry;

public static class RetryHelper
{
    private static readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt - 1)), // 100ms, 200ms, 400ms
            onRetry: (ex, ts, attempt, ctx) =>
            {
                Console.WriteLine($"[RetryHelper] Attempt {attempt}: Retrying after {ts.TotalMilliseconds} ms due to {ex.Message}");
            });

    public static async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        return await _retryPolicy.ExecuteAsync(action);
    }

    public static async Task ExecuteAsync(Func<Task> action)
    {
        await _retryPolicy.ExecuteAsync(action);
    }

    // Sync version
    private static readonly RetryPolicy _syncRetryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetry(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt - 1)),
            onRetry: (ex, ts, attempt, ctx) =>
            {
                Console.WriteLine($"[RetryHelper] [Sync] Attempt {attempt}: Retrying after {ts.TotalMilliseconds} ms due to {ex.Message}");
            });

    public static T Execute<T>(Func<T> action)
    {
        return _syncRetryPolicy.Execute(action);
    }
}
