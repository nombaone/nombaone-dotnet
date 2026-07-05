using System;
using System.Collections.Generic;

namespace NombaOne;

/// <summary>
/// A non-2xx response from the API. Carries everything the error envelope said:
/// the stable <see cref="Code"/> to branch on, the human <see cref="System.Exception.Message"/>,
/// an actionable <see cref="Hint"/> telling you exactly what to do next, a
/// <see cref="DocUrl"/> deep-linking into the error reference, per-field
/// validation errors on 422s (<see cref="Fields"/>), and the
/// <see cref="RequestId"/> to quote to support.
/// </summary>
/// <remarks>
/// The thrown <see cref="Exception.Message"/> already includes the hint, so the
/// fix arrives with the failure. Subtypes are keyed by HTTP status, so
/// <c>catch (NotFoundException)</c> reads naturally; branch on
/// <see cref="Code"/> (compared against <see cref="NombaoneErrorCodes"/>) for
/// finer-grained handling.
/// </remarks>
public class NombaoneApiException : NombaoneException
{
    /// <summary>The HTTP status code of the response.</summary>
    public int StatusCode { get; }

    /// <summary>
    /// The stable, machine-readable error code — branch on this. Compare with
    /// the constants on <see cref="NombaoneErrorCodes"/>; treat the set as open.
    /// </summary>
    public string Code { get; }

    /// <summary>Actionable, plain-English guidance on exactly what to do next.</summary>
    public string Hint { get; }

    /// <summary>Deep link to this code's entry in the public error reference.</summary>
    public string DocUrl { get; }

    /// <summary>
    /// Per-field validation errors (field path → messages), present on 422
    /// validation failures; <c>null</c> otherwise.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? Fields { get; }

    /// <summary>The request id (<c>meta.requestId</c>) — quote it to support.</summary>
    public string? RequestId { get; }

    /// <summary>Creates a new <see cref="NombaoneApiException"/>.</summary>
    /// <param name="message">The human-readable message from the API.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="code">The stable machine-readable error code.</param>
    /// <param name="hint">Actionable guidance; folded into the exception message.</param>
    /// <param name="docUrl">Deep link into the error reference.</param>
    /// <param name="fields">Per-field validation errors (422 only).</param>
    /// <param name="requestId">The request id to quote to support.</param>
    public NombaoneApiException(
        string message,
        int statusCode,
        string code,
        string? hint = null,
        string? docUrl = null,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? fields = null,
        string? requestId = null)
        : base(BuildMessage(message, hint))
    {
        StatusCode = statusCode;
        Code = code;
        Hint = hint ?? string.Empty;
        DocUrl = docUrl ?? string.Empty;
        Fields = fields;
        RequestId = requestId;
    }

    // Surface the hint in the thrown message itself — the fix should arrive with
    // the failure, without a docs tab.
    private static string BuildMessage(string message, string? hint) =>
        string.IsNullOrEmpty(hint) ? message : $"{message} — {hint}";
}

/// <summary>400 — the request could not be understood.</summary>
public sealed class BadRequestException : NombaoneApiException
{
    /// <summary>Creates a new <see cref="BadRequestException"/>.</summary>
    /// <inheritdoc cref="NombaoneApiException(string, int, string, string?, string?, IReadOnlyDictionary{string, IReadOnlyList{string}}?, string?)"/>
    public BadRequestException(string message, int statusCode, string code, string? hint = null, string? docUrl = null, IReadOnlyDictionary<string, IReadOnlyList<string>>? fields = null, string? requestId = null)
        : base(message, statusCode, code, hint, docUrl, fields, requestId) { }
}

/// <summary>401 — missing, invalid, revoked, or wrong-environment API key.</summary>
public sealed class AuthenticationException : NombaoneApiException
{
    /// <summary>Creates a new <see cref="AuthenticationException"/>.</summary>
    /// <inheritdoc cref="NombaoneApiException(string, int, string, string?, string?, IReadOnlyDictionary{string, IReadOnlyList{string}}?, string?)"/>
    public AuthenticationException(string message, int statusCode, string code, string? hint = null, string? docUrl = null, IReadOnlyDictionary<string, IReadOnlyList<string>>? fields = null, string? requestId = null)
        : base(message, statusCode, code, hint, docUrl, fields, requestId) { }
}

/// <summary>403 — a valid key that is not allowed (missing scope, foreign resource).</summary>
public sealed class PermissionDeniedException : NombaoneApiException
{
    /// <summary>Creates a new <see cref="PermissionDeniedException"/>.</summary>
    /// <inheritdoc cref="NombaoneApiException(string, int, string, string?, string?, IReadOnlyDictionary{string, IReadOnlyList{string}}?, string?)"/>
    public PermissionDeniedException(string message, int statusCode, string code, string? hint = null, string? docUrl = null, IReadOnlyDictionary<string, IReadOnlyList<string>>? fields = null, string? requestId = null)
        : base(message, statusCode, code, hint, docUrl, fields, requestId) { }
}

/// <summary>404 — no resource at that id in this environment.</summary>
public sealed class NotFoundException : NombaoneApiException
{
    /// <summary>Creates a new <see cref="NotFoundException"/>.</summary>
    /// <inheritdoc cref="NombaoneApiException(string, int, string, string?, string?, IReadOnlyDictionary{string, IReadOnlyList{string}}?, string?)"/>
    public NotFoundException(string message, int statusCode, string code, string? hint = null, string? docUrl = null, IReadOnlyDictionary<string, IReadOnlyList<string>>? fields = null, string? requestId = null)
        : base(message, statusCode, code, hint, docUrl, fields, requestId) { }
}

/// <summary>409 — conflicts with current state (including idempotency reuse / in-progress).</summary>
public sealed class ConflictException : NombaoneApiException
{
    /// <summary>Creates a new <see cref="ConflictException"/>.</summary>
    /// <inheritdoc cref="NombaoneApiException(string, int, string, string?, string?, IReadOnlyDictionary{string, IReadOnlyList{string}}?, string?)"/>
    public ConflictException(string message, int statusCode, string code, string? hint = null, string? docUrl = null, IReadOnlyDictionary<string, IReadOnlyList<string>>? fields = null, string? requestId = null)
        : base(message, statusCode, code, hint, docUrl, fields, requestId) { }
}

/// <summary>422 — one or more fields are invalid; see <see cref="NombaoneApiException.Fields"/>.</summary>
public sealed class ValidationException : NombaoneApiException
{
    /// <summary>Creates a new <see cref="ValidationException"/>.</summary>
    /// <inheritdoc cref="NombaoneApiException(string, int, string, string?, string?, IReadOnlyDictionary{string, IReadOnlyList{string}}?, string?)"/>
    public ValidationException(string message, int statusCode, string code, string? hint = null, string? docUrl = null, IReadOnlyDictionary<string, IReadOnlyList<string>>? fields = null, string? requestId = null)
        : base(message, statusCode, code, hint, docUrl, fields, requestId) { }
}

/// <summary>429 — slow down; retry after <see cref="RetryAfter"/> seconds.</summary>
public sealed class RateLimitException : NombaoneApiException
{
    /// <summary>Seconds until the current rate-limit window rolls over (<c>Retry-After</c>), if provided.</summary>
    public int? RetryAfter { get; }

    /// <summary>Your per-window request cap (<c>X-RateLimit-Limit</c>), if provided.</summary>
    public int? Limit { get; }

    /// <summary>Requests remaining in the current window (<c>X-RateLimit-Remaining</c>), if provided.</summary>
    public int? Remaining { get; }

    /// <summary>Creates a new <see cref="RateLimitException"/>.</summary>
    /// <param name="message">The human-readable message from the API.</param>
    /// <param name="statusCode">The HTTP status code (429).</param>
    /// <param name="code">The stable machine-readable error code.</param>
    /// <param name="hint">Actionable guidance; folded into the exception message.</param>
    /// <param name="docUrl">Deep link into the error reference.</param>
    /// <param name="requestId">The request id to quote to support.</param>
    /// <param name="retryAfter">Seconds until the window rolls over.</param>
    /// <param name="limit">The per-window request cap.</param>
    /// <param name="remaining">Requests remaining in the current window.</param>
    public RateLimitException(
        string message,
        int statusCode,
        string code,
        string? hint = null,
        string? docUrl = null,
        string? requestId = null,
        int? retryAfter = null,
        int? limit = null,
        int? remaining = null)
        : base(message, statusCode, code, hint, docUrl, fields: null, requestId: requestId)
    {
        RetryAfter = retryAfter;
        Limit = limit;
        Remaining = remaining;
    }
}

/// <summary>5xx — something failed on NombaOne's side. Safe to retry (the SDK already did).</summary>
public sealed class ServerException : NombaoneApiException
{
    /// <summary>Creates a new <see cref="ServerException"/>.</summary>
    /// <inheritdoc cref="NombaoneApiException(string, int, string, string?, string?, IReadOnlyDictionary{string, IReadOnlyList{string}}?, string?)"/>
    public ServerException(string message, int statusCode, string code, string? hint = null, string? docUrl = null, IReadOnlyDictionary<string, IReadOnlyList<string>>? fields = null, string? requestId = null)
        : base(message, statusCode, code, hint, docUrl, fields, requestId) { }
}
