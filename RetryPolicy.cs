using NLog;

namespace SaturnClient;

public static class RetryPolicy
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static async Task<T> RetryOnConditionAsync<T>(
        Func<Task<T>> operation,
        Func<T, bool> condition,
        int maxRetryCount = 3,
        TimeSpan? delay = null)
    {
        var retries = 0;
        while (true)
        {
            try
            {
                var result = await operation();
                if (!condition(result) || retries >= maxRetryCount)
                {
                    Logger.Info($"Operation successful or max retries reached. Retries: {retries}");
                    return result;
                }
                Logger.Warn($"Condition not met for operation. Retrying... Attempt: {retries + 1} of {maxRetryCount}");
            }
            catch (Exception ex)
            {
                if (retries >= maxRetryCount)
                {
                    Logger.Error(ex, $"Operation failed after {maxRetryCount} attempts.");
                    throw;
                }
                Logger.Warn(ex, $"Exception occurred during operation. Retrying... Attempt: {retries + 1} of {maxRetryCount}");
            }
            retries++;
            await Task.Delay(delay ?? TimeSpan.FromSeconds(2));
        }
    }
}

