using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NombaOne;
using NombaOne.Internal;

namespace NombaOne.Tests.Helpers;

/// <summary>A single recorded outbound request (method, url, headers, body).</summary>
internal sealed class RecordedRequest
{
    public required HttpMethod Method { get; init; }
    public required string Path { get; init; }
    public required string Url { get; init; }
    public string? Body { get; init; }
    public required IReadOnlyDictionary<string, string> Headers { get; init; }

    public string? Header(string name) => Headers.TryGetValue(name, out var value) ? value : null;
}

/// <summary>
/// A test <see cref="HttpMessageHandler"/> that records every request and
/// returns canned responses from a responder keyed by attempt index.
/// </summary>
internal sealed class MockHttpHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, int, CancellationToken, Task<HttpResponseMessage>> _responder;
    private int _attempt;

    public MockHttpHandler(Func<HttpRequestMessage, int, CancellationToken, Task<HttpResponseMessage>> responder) =>
        _responder = responder;

    public List<RecordedRequest> Requests { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync().ConfigureAwait(false);

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers)
        {
            headers[header.Key] = string.Join(",", header.Value);
        }

        if (request.Content is not null)
        {
            foreach (var header in request.Content.Headers)
            {
                headers[header.Key] = string.Join(",", header.Value);
            }
        }

        Requests.Add(new RecordedRequest
        {
            Method = request.Method,
            Path = request.RequestUri!.AbsolutePath,
            Url = request.RequestUri!.ToString(),
            Body = body,
            Headers = headers,
        });

        return await _responder(request, _attempt++, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Builders for canned HTTP responses.</summary>
internal static class Responses
{
    public static HttpResponseMessage Json(HttpStatusCode status, string json, Action<HttpResponseMessage>? configure = null)
    {
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        configure?.Invoke(response);
        return response;
    }

    public static HttpResponseMessage Text(HttpStatusCode status, string body, string contentType = "text/html")
    {
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, contentType),
        };
    }
}

/// <summary>Canonical envelope JSON strings for tests.</summary>
internal static class Envelopes
{
    public static string Success(string id = "nbo000000000001cus", long amountInKobo = 250_000, string requestId = "req_success") =>
        $"{{\"success\":true,\"statusCode\":200,\"data\":{{\"id\":\"{id}\",\"amountInKobo\":{amountInKobo}}},\"meta\":{{\"requestId\":\"{requestId}\"}}}}";

    public static string SuccessList(string requestId = "req_list", bool hasMore = false, string? nextCursor = null, int limit = 20) =>
        "{\"success\":true,\"statusCode\":200,\"data\":[{\"id\":\"nbo000000000001cus\",\"amountInKobo\":100}]," +
        $"\"pagination\":{{\"limit\":{limit},\"hasMore\":{(hasMore ? "true" : "false")},\"nextCursor\":{(nextCursor is null ? "null" : $"\"{nextCursor}\"")}}}," +
        $"\"meta\":{{\"requestId\":\"{requestId}\"}}}}";

    public static string Error(string code, string message = "Something went wrong", string hint = "Fix it like so", string docUrl = "https://docs.nombaone.xyz/errors#CODE", string requestId = "req_error", string? fieldsJson = null)
    {
        var fields = fieldsJson is null ? string.Empty : $",\"fields\":{fieldsJson}";
        return $"{{\"success\":false,\"statusCode\":400,\"error\":{{\"code\":\"{code}\",\"message\":\"{message}\",\"hint\":\"{hint}\",\"docUrl\":\"{docUrl}\"{fields}}},\"meta\":{{\"requestId\":\"{requestId}\"}}}}";
    }
}

/// <summary>Factory that wires a <see cref="HttpTransport"/> to a mock handler with instant retries.</summary>
internal static class TestTransport
{
    public static (HttpTransport Transport, MockHttpHandler Handler) Create(
        Func<HttpRequestMessage, int, CancellationToken, Task<HttpResponseMessage>> responder,
        int maxRetries = 2,
        TimeSpan? timeout = null,
        string apiKey = "nbo_sandbox_test",
        string baseUrl = "https://api.test",
        IReadOnlyDictionary<string, string>? defaultHeaders = null)
    {
        var handler = new MockHttpHandler(responder);
        var httpClient = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        var transport = new HttpTransport(
            httpClient, apiKey, baseUrl, timeout ?? TimeSpan.FromSeconds(30), maxRetries, defaultHeaders)
        {
            SleepAsync = (_, _) => Task.CompletedTask,
            BackoffStrategy = _ => TimeSpan.Zero,
        };
        return (transport, handler);
    }
}

/// <summary>A minimal entity for transport tests.</summary>
internal sealed class TestResource : NombaoneEntity
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("amountInKobo")]
    public long AmountInKobo { get; init; }
}
