using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Microsoft.SemanticKernel.Resilience;

/// <summary>
/// Helpers for retrying failed HTTP requests when calling OpenAI endpoints.
/// </summary>
public static partial class SemanticKernelResilienceExtensions
{
    private const int DefaultTimeOutInSeconds = 20;
    private const int MaxRetries = 10;

    /// <summary>
    /// Executes the supplied asynchronous action with retry logic for retryable <see cref="HttpRequestException"/> instances.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="task">The asynchronous task to execute.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result produced by <paramref name="task"/>.</returns>
    public static Task<TResult> WithRetryAsync<TResult>(this Task<TResult> task, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        return ExecuteWithRetryAsync(task, logger, cancellationToken);
    }

    private static async Task<TResult> ExecuteWithRetryAsync<TResult>(Task<TResult> task, ILogger? logger, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await task.ConfigureAwait(false);
            }
            catch (HttpOperationException ex) when (IsExceptionRetryable(ex) && attempt < MaxRetries)
            {
                var delay = GetDelay(attempt, ex);
                logger?.LogDebug(ex, "Request failed. Waiting {timeSpan} before next retry. Retry attempt {retryCount}/{maxRetries}.", delay, attempt, MaxRetries);

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static bool IsExceptionRetryable(HttpOperationException exception)
    {
        return RateLimitReachedRegex().IsMatch(exception.Message);
    }

    private static TimeSpan GetDelay(int attempt, Exception exception)
    {
        var seconds = TryExtractWaitSecondsFromExceptionMessage(exception.Message, out var waitSeconds) ? waitSeconds : DefaultTimeOutInSeconds;
        if (seconds <= 1)
        {
            seconds = 1;
        }

        // First retry uses the exact wait time suggested by the service, subsequent retries use exponential backoff.
        return attempt == 1 ? TimeSpan.FromSeconds(seconds) : TimeSpan.FromSeconds(Math.Pow(2, attempt));
    }

    private static bool TryExtractWaitSecondsFromExceptionMessage(string exceptionMessage, out double waitSeconds)
    {
        var match = PleaseTryAgainRegex().Match(exceptionMessage);
        if (match.Success && double.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var parsedValue))
        {
            waitSeconds = parsedValue;
            return true;
        }

        waitSeconds = default;
        return false;
    }

    // HTTP 429 (tokens: rate_limit_exceeded)
    //
    // Rate limit reached for gpt-4o in organization org-xxxxxxxxxxxxxxxxxxxxxxxx on tokens per min (TPM): Limit 30000, Used 27855, Requested 5405. Please try again in 6.52s. Visit https://platform.openai.com/account/rate-limits to learn more.

    [GeneratedRegex(@"HTTP 429 \(tokens: rate_limit_exceeded\)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RateLimitReachedRegex();

    [GeneratedRegex(@"Please try again in (\d+\.\d+)s\.", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PleaseTryAgainRegex();
}