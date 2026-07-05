using System;
using System.Collections.Generic;
using System.Net.Http;

namespace NombaOne;

/// <summary>
/// Configuration for a <see cref="Nombaone"/> client. Everything is optional;
/// the only value the client truly needs is an API key, which defaults to the
/// <c>NOMBAONE_API_KEY</c> environment variable.
/// </summary>
public sealed class NombaoneOptions
{
    /// <summary>
    /// Your secret API key (<c>nbo_sandbox_…</c> or <c>nbo_live_…</c>). Defaults
    /// to the <c>NOMBAONE_API_KEY</c> environment variable. Server-side only —
    /// never ship it in a client application.
    /// </summary>
    public string? ApiKey { get; init; }

    /// <summary>
    /// Override the API origin (no <c>/v1</c>). Defaults to the host matching
    /// your key's environment. Required if the key prefix is unrecognized.
    /// </summary>
    public string? BaseUrl { get; init; }

    /// <summary>Per-attempt timeout. Default 30 seconds.</summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Automatic retries for network failures, timeouts, 408/429/5xx, and
    /// in-flight idempotency conflicts. Default 2 (3 attempts total). Retries of
    /// a POST always reuse the same <c>Idempotency-Key</c>.
    /// </summary>
    public int? MaxRetries { get; init; }

    /// <summary>
    /// Bring your own <see cref="System.Net.Http.HttpClient"/> (for custom
    /// handlers, proxies, connection pooling, or tests). When supplied, the SDK
    /// uses it as-is and does not dispose it; per-attempt timeouts are enforced
    /// via cancellation rather than <see cref="HttpClient.Timeout"/>.
    /// </summary>
    public HttpClient? HttpClient { get; init; }

    /// <summary>Extra headers sent on every request.</summary>
    public IReadOnlyDictionary<string, string>? DefaultHeaders { get; init; }
}
