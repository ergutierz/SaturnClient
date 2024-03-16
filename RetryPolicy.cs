using NLog;

namespace SaturnClient;

/// <summary>
/// Provides a static class for implementing retry policies on asynchronous operations.
/// </summary>
public static class RetryPolicy
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Executes an asynchronous operation with a retry policy based on a specified condition.
    /// </summary>
    /// <param name="operation">The asynchronous operation to execute, which returns a Task of type T.</param>
    /// <param name="condition">The condition to evaluate after each operation execution to determine if a retry should occur.</param>
    /// <param name="maxRetryCount">The maximum number of retries allowed (default is 3).</param>
    /// <param name="delay">The delay between retries (default is 2 seconds).</param>
    /// <returns>The result of the operation of type T.</returns>
    /// <exception cref="Exception">Throws the last exception if the maximum number of retries is reached and the operation still fails.</exception>
    /// <remarks>
    /// This method logs information about each retry attempt and the success or failure of the operation.
    /// </remarks>
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
