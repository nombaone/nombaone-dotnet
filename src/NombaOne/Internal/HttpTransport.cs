using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NombaOne.Internal;

/// <summary>
/// The SDK's HTTP engine. Executes one logical API call: builds the request,
/// runs the retry loop, parses the envelope, and either returns the unwrapped
/// result or throws a typed exception.
/// </summary>
/// <remarks>
/// Money-safety invariants live here and nowhere else:
/// <list type="bullet">
///   <item><description>The <c>Idempotency-Key</c> for a POST is computed <b>once, before the retry loop</b>, so every automatic retry replays the same logical operation instead of creating a new one.</description></item>
///   <item><description>A caller-initiated cancellation is never retried; only network failures, timeouts, 408/429/5xx, and our own in-flight idempotency conflict are.</description></item>
/// </list>
/// </remarks>
internal sealed class HttpTransport
{
    private const string ApiPrefix = "/v1";

    private static readonly HashSet<int> RetryableStatuses = new() { 408, 429, 500, 502, 503, 504 };

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly TimeSpan _timeout;
    private readonly int _maxRetries;
    private readonly IReadOnlyDictionary<string, string>? _defaultHeaders;
    private readonly JsonSerializerOptions _json;

    internal HttpTransport(
        HttpClient httpClient,
        string apiKey,
        string baseUrl,
        TimeSpan timeout,
        int maxRetries,
        IReadOnlyDictionary<string, string>? defaultHeaders)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _baseUrl = baseUrl;
        _timeout = timeout;
        _maxRetries = maxRetries;
        _defaultHeaders = defaultHeaders;
        _json = NombaoneJson.Options;
    }

    /// <summary>Backoff strategy; overridable in tests to remove jitter/delay.</summary>
    internal Func<int, TimeSpan> BackoffStrategy { get; set; } = Backoff.FullJitter;

    /// <summary>Sleep implementation; overridable in tests to make retries instant.</summary>
    internal Func<TimeSpan, CancellationToken, Task> SleepAsync { get; set; } = (delay, ct) => Task.Delay(delay, ct);

    internal async Task<TransportResult<T>> SendAsync<T>(RequestSpec spec, CancellationToken cancellationToken)
    {
        var options = spec.Options;
        var timeout = options?.Timeout ?? _timeout;
        var maxRetries = Math.Max(0, options?.MaxRetries ?? _maxRetries);
        var isPost = spec.Method == HttpVerbs.Post;

        var url = _baseUrl + ApiPrefix + spec.Path + QueryString.Build(spec.Query);

        // Compute the idempotency key ONCE, before the retry loop (POST only), so
        // every automatic retry replays the same logical operation.
        var idempotencyKey = isPost ? (options?.IdempotencyKey ?? Guid.NewGuid().ToString("D")) : null;
        var bodyJson = spec.Body is null ? null : JsonSerializer.Serialize(spec.Body, _json);

        for (var attempt = 0; ; attempt++)
        {
            HttpResponseMessage response;
            string responseBody;
            try
            {
                (response, responseBody) = await SingleAttemptAsync(
                    spec.Method, url, bodyJson, idempotencyKey, options?.Headers, timeout, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // A caller cancellation is a decision, not a fault — never retried.
            }
            catch (OperationCanceledException ex)
            {
                var timeoutError = new NombaoneTimeoutException(
                    $"Request timed out after {timeout.TotalSeconds:0.###}s.", ex);
                if (attempt >= maxRetries)
                {
                    throw timeoutError;
                }

                await SleepAsync(BackoffStrategy(attempt), cancellationToken).ConfigureAwait(false);
                continue;
            }
            catch (HttpRequestException ex)
            {
                var connectionError = new NombaoneConnectionException(
                    "Failed to reach the NombaOne API.", ex);
                if (attempt >= maxRetries)
                {
                    throw connectionError;
                }

                await SleepAsync(BackoffStrategy(attempt), cancellationToken).ConfigureAwait(false);
                continue;
            }

            try
            {
                var status = (int)response.StatusCode;
                var headerRequestId = Backoff.FirstHeaderValue(response.Headers, "X-Request-Id");

                if (response.IsSuccessStatusCode)
                {
                    return ParseSuccess<T>(status, responseBody, response, headerRequestId);
                }

                var errorEnvelope = TryDeserialize<ErrorEnvelope>(responseBody);
                var apiError = ErrorFactory.FromResponse(status, errorEnvelope, response.Headers, headerRequestId);

                if (attempt < maxRetries && IsRetryable(status, apiError))
                {
                    var delay = Backoff.RetryAfter(response.Headers) ?? BackoffStrategy(attempt);
                    await SleepAsync(delay, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                throw apiError;
            }
            finally
            {
                response.Dispose();
            }
        }
    }

    private async Task<(HttpResponseMessage Response, string Body)> SingleAttemptAsync(
        HttpMethod method,
        string url,
        string? bodyJson,
        string? idempotencyKey,
        IReadOnlyDictionary<string, string?>? perCallHeaders,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        using var request = new HttpRequestMessage(method, url);
        if (bodyJson is not null)
        {
            request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
        }

        ApplyHeaders(request, method, idempotencyKey, perCallHeaders);

        var response = await _httpClient
            .SendAsync(request, HttpCompletionOption.ResponseContentRead, timeoutCts.Token)
            .ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return (response, body);
    }

    private void ApplyHeaders(
        HttpRequestMessage request,
        HttpMethod method,
        string? idempotencyKey,
        IReadOnlyDictionary<string, string?>? perCallHeaders)
    {
        // SDK-managed headers first, then client defaults, then per-call (later
        // layers override earlier ones by name; a null per-call value removes).
        var merged = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Authorization"] = "Bearer " + _apiKey,
            ["Accept"] = "application/json",
            ["User-Agent"] = NombaoneVersion.UserAgent,
        };
        if (idempotencyKey is not null)
        {
            merged["Idempotency-Key"] = idempotencyKey;
        }

        if (_defaultHeaders is not null)
        {
            foreach (var header in _defaultHeaders)
            {
                merged[header.Key] = header.Value;
            }
        }

        if (perCallHeaders is not null)
        {
            foreach (var header in perCallHeaders)
            {
                if (header.Value is null)
                {
                    merged.Remove(header.Key);
                }
                else
                {
                    merged[header.Key] = header.Value;
                }
            }
        }

        foreach (var header in merged)
        {
            if (header.Value is null)
            {
                continue;
            }

            // Content-Type belongs on the content, not the request headers.
            if (string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                if (request.Content is not null)
                {
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(header.Value);
                }

                continue;
            }

            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
    }

    private TransportResult<T> ParseSuccess<T>(
        int status,
        string body,
        HttpResponseMessage response,
        string? headerRequestId)
    {
        SuccessEnvelope<T>? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<SuccessEnvelope<T>>(body, _json);
        }
        catch (JsonException ex)
        {
            throw new ServerException(
                "The API returned a response body that could not be decoded.",
                status,
                NombaoneErrorCodes.SystemInternalError,
                hint: ex.Message,
                requestId: headerRequestId);
        }

        var data = envelope is null ? default : envelope.Data;
        if (envelope is null || !envelope.Success || data is null)
        {
            throw new ServerException(
                "The API response was not a valid NombaOne envelope.",
                status,
                NombaoneErrorCodes.SystemInternalError,
                requestId: string.IsNullOrEmpty(envelope?.Meta?.RequestId) ? headerRequestId : envelope!.Meta!.RequestId);
        }

        var requestId = string.IsNullOrEmpty(envelope.Meta?.RequestId) ? headerRequestId : envelope.Meta!.RequestId;
        var nombaoneResponse = new NombaoneResponse(status, requestId, response.Headers);

        var pagination = envelope.Pagination is null
            ? null
            : new NombaonePagination(envelope.Pagination.Limit, envelope.Pagination.HasMore, envelope.Pagination.NextCursor);

        if (data is NombaoneEntity entity)
        {
            entity.RawResponse = nombaoneResponse;
        }

        return new TransportResult<T>(data, pagination, nombaoneResponse);
    }

    private static bool IsRetryable(int status, NombaoneApiException error) =>
        RetryableStatuses.Contains(status) ||
        (status == 409 && error.Code == NombaoneErrorCodes.IdempotencyInProgress);

    private T? TryDeserialize<T>(string body)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(body, _json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
