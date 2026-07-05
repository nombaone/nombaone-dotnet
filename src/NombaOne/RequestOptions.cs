using System;
using System.Collections.Generic;

namespace NombaOne;

/// <summary>
/// Per-call overrides accepted as the optional last argument of every SDK
/// method. Anything left unset falls back to the client-level configuration.
/// </summary>
public sealed class RequestOptions
{
    /// <summary>
    /// Overrides the auto-generated <c>Idempotency-Key</c> header (POST requests
    /// only). The SDK generates a fresh key per call and <b>reuses it across
    /// automatic retries</b>, so a network blip can never double-charge. Pass
    /// your own stable key when the operation must stay idempotent across
    /// process restarts (for example a payout keyed by your own transaction id).
    /// </summary>
    public string? IdempotencyKey { get; init; }

    /// <summary>
    /// Extra headers for this request, merged over the SDK defaults. A
    /// <c>null</c> value removes a default header for this one call.
    /// </summary>
    public IReadOnlyDictionary<string, string?>? Headers { get; init; }

    /// <summary>Per-attempt timeout for this call, overriding the client default.</summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>Retry budget for this call, overriding the client default.</summary>
    public int? MaxRetries { get; init; }
}
