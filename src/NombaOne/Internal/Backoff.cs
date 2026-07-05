using System;
using System.Globalization;
using System.Net.Http.Headers;
#if !NET8_0_OR_GREATER
using System.Threading;
#endif

namespace NombaOne.Internal;

/// <summary>Retry-timing helpers: full-jitter backoff and <c>Retry-After</c> parsing.</summary>
internal static class Backoff
{
    private const double BaseMs = 500;
    private const double MaxMs = 8_000;

    /// <summary>
    /// Full-jitter exponential backoff: a random delay in
    /// <c>[0, min(8s, 500ms · 2^attempt))</c>. Jitter prevents a fleet of
    /// retrying clients from stampeding the API in lockstep.
    /// </summary>
    internal static TimeSpan FullJitter(int attempt)
    {
        var ceiling = Math.Min(MaxMs, BaseMs * Math.Pow(2, attempt));
        return TimeSpan.FromMilliseconds(NextDouble() * ceiling);
    }

    /// <summary>
    /// Parse a <c>Retry-After</c> header (delta-seconds or HTTP-date) into a
    /// delay, or <c>null</c> when absent/unparseable/in the past.
    /// </summary>
    internal static TimeSpan? RetryAfter(HttpResponseHeaders headers)
    {
        var raw = FirstHeaderValue(headers, "Retry-After");
        if (string.IsNullOrEmpty(raw))
        {
            return null;
        }

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
        {
            return seconds < 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(seconds);
        }

        if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var when))
        {
            var delta = when - DateTimeOffset.UtcNow;
            return delta > TimeSpan.Zero ? delta : TimeSpan.Zero;
        }

        return null;
    }

    /// <summary>Read the first value of a response header, or <c>null</c> if absent.</summary>
    internal static string? FirstHeaderValue(HttpResponseHeaders headers, string name)
    {
        if (!headers.TryGetValues(name, out var values))
        {
            return null;
        }

        foreach (var value in values)
        {
            return value;
        }

        return null;
    }

    /// <summary>Read an integer response header, or <c>null</c> if absent/unparseable.</summary>
    internal static int? IntHeaderValue(HttpResponseHeaders headers, string name)
    {
        var raw = FirstHeaderValue(headers, name);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    private static double NextDouble()
    {
#if NET8_0_OR_GREATER
        return Random.Shared.NextDouble();
#else
        return ThreadLocalRandom.Value!.NextDouble();
#endif
    }

#if !NET8_0_OR_GREATER
    private static readonly ThreadLocal<Random> ThreadLocalRandom = new(() => new Random());
#endif
}
