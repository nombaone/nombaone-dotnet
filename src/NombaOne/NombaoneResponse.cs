using System.Net.Http.Headers;

namespace NombaOne;

/// <summary>
/// Metadata about the raw HTTP response behind a returned resource — the status
/// code, the <see cref="RequestId"/> to quote to support, and the response
/// headers (rate-limit counters, <c>X-Request-Id</c>, and so on). Reachable via
/// <see cref="NombaoneEntity.RawResponse"/> on any returned object.
/// </summary>
public sealed class NombaoneResponse
{
    /// <summary>The HTTP status code of the response.</summary>
    public int StatusCode { get; }

    /// <summary>The request id (<c>meta.requestId</c> / <c>X-Request-Id</c>), if present.</summary>
    public string? RequestId { get; }

    /// <summary>The raw response headers.</summary>
    public HttpResponseHeaders Headers { get; }

    internal NombaoneResponse(int statusCode, string? requestId, HttpResponseHeaders headers)
    {
        StatusCode = statusCode;
        RequestId = requestId;
        Headers = headers;
    }
}
