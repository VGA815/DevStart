using Microsoft.Extensions.Logging;
using System.Net;

namespace DevStart.Infrastructure.Payments
{
    /// <summary>
    /// Adds resilience to YooKassa HTTP calls: bounded retries with exponential backoff + jitter on
    /// transient failures (HTTP 408/429/5xx, network errors, timeouts), honouring the server's
    /// <c>Retry-After</c> header. Retrying is safe because every request is either a GET or carries an
    /// <c>Idempotence-Key</c>. The request body is buffered so it can be replayed on each attempt.
    /// </summary>
    internal sealed class YooKassaResilienceHandler(ILogger<YooKassaResilienceHandler> logger) : DelegatingHandler
    {
        private const int MaxRetries = 3;
        private static readonly TimeSpan BaseDelay = TimeSpan.FromMilliseconds(500);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            byte[]? bufferedBody = request.Content is null
                ? null
                : await request.Content.ReadAsByteArrayAsync(cancellationToken);
            List<KeyValuePair<string, IEnumerable<string>>>? contentHeaders =
                request.Content?.Headers.ToList();

            for (int attempt = 0; ; attempt++)
            {
                using HttpRequestMessage attemptRequest = Clone(request, bufferedBody, contentHeaders);

                try
                {
                    HttpResponseMessage response = await base.SendAsync(attemptRequest, cancellationToken);

                    if (attempt >= MaxRetries || !IsTransientStatus(response.StatusCode))
                    {
                        return response;
                    }

                    TimeSpan delay = GetRetryAfter(response) ?? Backoff(attempt);
                    logger.LogWarning(
                        "YooKassa returned {StatusCode}; retry {Attempt}/{Max} after {DelayMs}ms.",
                        (int)response.StatusCode, attempt + 1, MaxRetries, (int)delay.TotalMilliseconds);
                    response.Dispose();
                    await Task.Delay(delay, cancellationToken);
                }
                catch (Exception ex) when (
                    attempt < MaxRetries
                    && !cancellationToken.IsCancellationRequested
                    && ex is HttpRequestException or TaskCanceledException)
                {
                    TimeSpan delay = Backoff(attempt);
                    logger.LogWarning(ex,
                        "YooKassa request failed; retry {Attempt}/{Max} after {DelayMs}ms.",
                        attempt + 1, MaxRetries, (int)delay.TotalMilliseconds);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        private static HttpRequestMessage Clone(
            HttpRequestMessage request,
            byte[]? bufferedBody,
            List<KeyValuePair<string, IEnumerable<string>>>? contentHeaders)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Version = request.Version,
            };

            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (bufferedBody is not null)
            {
                clone.Content = new ByteArrayContent(bufferedBody);
                if (contentHeaders is not null)
                {
                    foreach (KeyValuePair<string, IEnumerable<string>> header in contentHeaders)
                    {
                        clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            }

            return clone;
        }

        private static bool IsTransientStatus(HttpStatusCode statusCode) =>
            statusCode == HttpStatusCode.RequestTimeout
            || statusCode == HttpStatusCode.TooManyRequests
            || (int)statusCode >= 500;

        private static TimeSpan? GetRetryAfter(HttpResponseMessage response)
        {
            System.Net.Http.Headers.RetryConditionHeaderValue? retryAfter = response.Headers.RetryAfter;
            if (retryAfter is null)
            {
                return null;
            }
            if (retryAfter.Delta is { } delta)
            {
                return delta;
            }
            if (retryAfter.Date is { } date)
            {
                TimeSpan until = date - DateTimeOffset.UtcNow;
                return until > TimeSpan.Zero ? until : TimeSpan.Zero;
            }
            return null;
        }

        private static TimeSpan Backoff(int attempt)
        {
            double exponential = BaseDelay.TotalMilliseconds * Math.Pow(2, attempt);
            double jitter = Random.Shared.Next(0, 250);
            return TimeSpan.FromMilliseconds(exponential + jitter);
        }
    }
}
