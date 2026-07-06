using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace NombaOne.Webhooks;

/// <summary>
/// Verifies and parses NombaOne webhook deliveries. It needs only the endpoint's
/// signing secret — never an API key — so a webhook receiver can use it alone.
/// </summary>
/// <remarks>
/// <b>Feed it the exact raw request body.</b> Re-encoding JSON can reorder keys
/// and change bytes, which breaks the signature — capture the body before any
/// framework parses it (for example, read the raw request stream). The overloads
/// taking <c>byte[]</c> verify over the exact bytes received and are preferred.
/// </remarks>
public static class WebhookVerifier
{
    /// <summary>The default maximum age (in either direction) of a delivery's timestamp.</summary>
    public static readonly TimeSpan DefaultTolerance = TimeSpan.FromSeconds(300);

    /// <summary>
    /// Verify a delivery's signature and timestamp, then parse and return the
    /// typed event. This is the one call your handler needs. Delivery is
    /// at-least-once — after verification, dedupe on <c>Event.Id</c> before acting.
    /// </summary>
    /// <param name="payload">The exact raw request body bytes.</param>
    /// <param name="signatureHeader">The <c>X-Nombaone-Signature</c> header value.</param>
    /// <param name="secret">The endpoint's signing secret (shown once at creation).</param>
    /// <param name="tolerance">Maximum timestamp age; defaults to <see cref="DefaultTolerance"/>.</param>
    /// <exception cref="WebhookVerificationException">On a missing/malformed header, a stale/future timestamp, an invalid signature, a missing secret, or a non-JSON body.</exception>
    public static WebhookEvent ConstructEvent(byte[] payload, string signatureHeader, string secret, TimeSpan? tolerance = null)
    {
        VerifySignature(payload, signatureHeader, secret, tolerance);
        return WebhookEvent.Parse(Encoding.UTF8.GetString(payload));
    }

    /// <inheritdoc cref="ConstructEvent(byte[], string, string, TimeSpan?)"/>
    /// <param name="payload">The exact raw request body string.</param>
    /// <param name="signatureHeader">The <c>X-Nombaone-Signature</c> header value.</param>
    /// <param name="secret">The endpoint's signing secret.</param>
    /// <param name="tolerance">Maximum timestamp age; defaults to <see cref="DefaultTolerance"/>.</param>
    public static WebhookEvent ConstructEvent(string payload, string signatureHeader, string secret, TimeSpan? tolerance = null) =>
        ConstructEvent(Encoding.UTF8.GetBytes(payload), signatureHeader, secret, tolerance);

    /// <summary>Verify only (no parse). Throws with a distinct message per failure mode; returns on success.</summary>
    /// <param name="payload">The exact raw request body bytes.</param>
    /// <param name="signatureHeader">The <c>X-Nombaone-Signature</c> header value.</param>
    /// <param name="secret">The endpoint's signing secret.</param>
    /// <param name="tolerance">Maximum timestamp age; defaults to <see cref="DefaultTolerance"/>.</param>
    /// <exception cref="WebhookVerificationException">On any verification failure.</exception>
    public static void VerifySignature(byte[] payload, string signatureHeader, string secret, TimeSpan? tolerance = null)
    {
        if (string.IsNullOrEmpty(signatureHeader))
        {
            throw new WebhookVerificationException(
                "Missing X-Nombaone-Signature header — is this request really from NombaOne?");
        }

        if (string.IsNullOrEmpty(secret))
        {
            throw new WebhookVerificationException(
                "Missing signing secret — pass the secret shown when the endpoint was created.");
        }

        var (timestamp, signatures) = ParseSignatureHeader(signatureHeader);

        if (!long.TryParse(timestamp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timestampSeconds))
        {
            throw new WebhookVerificationException(
                "Malformed X-Nombaone-Signature header — `t` is not a unix timestamp.");
        }

        var allowed = (tolerance ?? DefaultTolerance).TotalSeconds;
        var ageSeconds = Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timestampSeconds);
        if (ageSeconds > allowed)
        {
            throw new WebhookVerificationException(
                $"Webhook timestamp is outside the allowed tolerance ({ageSeconds}s away, limit {allowed:0}s) — possible replay, or severe clock skew.");
        }

        var expected = ComputeSignature(secret, timestamp, payload);
        var expectedBytes = Encoding.ASCII.GetBytes(expected);

        // Multiple v1 entries are legal during secret rotation — any match passes.
        foreach (var candidate in signatures)
        {
            if (FixedTimeEquals(Encoding.ASCII.GetBytes(candidate), expectedBytes))
            {
                return;
            }
        }

        throw new WebhookVerificationException(
            "Webhook signature verification failed — check you are using this endpoint's current signing secret " +
            "and the exact raw request body (no re-serialization).");
    }

    /// <inheritdoc cref="VerifySignature(byte[], string, string, TimeSpan?)"/>
    /// <param name="payload">The exact raw request body string.</param>
    /// <param name="signatureHeader">The <c>X-Nombaone-Signature</c> header value.</param>
    /// <param name="secret">The endpoint's signing secret.</param>
    /// <param name="tolerance">Maximum timestamp age; defaults to <see cref="DefaultTolerance"/>.</param>
    public static void VerifySignature(string payload, string signatureHeader, string secret, TimeSpan? tolerance = null) =>
        VerifySignature(Encoding.UTF8.GetBytes(payload), signatureHeader, secret, tolerance);

    /// <summary>
    /// Build a valid <c>X-Nombaone-Signature</c> header for a payload — for
    /// testing your own handler without waiting on a real delivery.
    /// </summary>
    /// <param name="payload">The raw body bytes to sign.</param>
    /// <param name="secret">The signing secret.</param>
    /// <param name="timestamp">The timestamp to embed; defaults to now.</param>
    public static string GenerateTestHeader(byte[] payload, string secret, DateTimeOffset? timestamp = null)
    {
        var seconds = (timestamp ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        return $"t={seconds},v1={ComputeSignature(secret, seconds, payload)}";
    }

    /// <inheritdoc cref="GenerateTestHeader(byte[], string, DateTimeOffset?)"/>
    /// <param name="payload">The raw body string to sign.</param>
    /// <param name="secret">The signing secret.</param>
    /// <param name="timestamp">The timestamp to embed; defaults to now.</param>
    public static string GenerateTestHeader(string payload, string secret, DateTimeOffset? timestamp = null) =>
        GenerateTestHeader(Encoding.UTF8.GetBytes(payload), secret, timestamp);

    // hex(HMAC_SHA256(secret, "{t}." + rawBody)).
    private static string ComputeSignature(string secret, string timestamp, byte[] payload)
    {
        var prefix = Encoding.UTF8.GetBytes(timestamp + ".");
        var message = new byte[prefix.Length + payload.Length];
        Buffer.BlockCopy(prefix, 0, message, 0, prefix.Length);
        Buffer.BlockCopy(payload, 0, message, prefix.Length, payload.Length);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return ToHexLower(hmac.ComputeHash(message));
    }

    private static (string Timestamp, IReadOnlyList<string> Signatures) ParseSignatureHeader(string header)
    {
        string? timestamp = null;
        var signatures = new List<string>();

        foreach (var pair in header.Split(','))
        {
            var index = pair.IndexOf('=');
            if (index < 0)
            {
                continue;
            }

            var key = pair.Substring(0, index).Trim();
            var value = pair.Substring(index + 1).Trim();
            if (key == "t")
            {
                timestamp = value;
            }
            else if (key == "v1" && value.Length > 0)
            {
                signatures.Add(value);
            }
        }

        if (timestamp is null || signatures.Count == 0)
        {
            throw new WebhookVerificationException(
                "Malformed X-Nombaone-Signature header — expected \"t=<unix>,v1=<hex>\".");
        }

        return (timestamp, signatures);
    }

    private static bool FixedTimeEquals(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        var difference = 0;
        for (var i = 0; i < a.Length; i++)
        {
            difference |= a[i] ^ b[i];
        }

        return difference == 0;
    }

    private static string ToHexLower(byte[] bytes)
    {
        const string hex = "0123456789abcdef";
        var chars = new char[bytes.Length * 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            chars[i * 2] = hex[bytes[i] >> 4];
            chars[(i * 2) + 1] = hex[bytes[i] & 0xF];
        }

        return new string(chars);
    }
}
