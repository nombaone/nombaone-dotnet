using System.Collections.Generic;
using System.Net.Http.Headers;

namespace NombaOne.Internal;

/// <summary>
/// Builds the right <see cref="NombaoneApiException"/> subclass from a non-2xx
/// response. A body that is not a usable error envelope degrades gracefully to
/// the default code for the status — it never crashes the parser.
/// </summary>
internal static class ErrorFactory
{
    internal static NombaoneApiException FromResponse(
        int status,
        ErrorEnvelope? envelope,
        HttpResponseHeaders headers,
        string? headerRequestId)
    {
        var error = envelope?.Error;
        var code = string.IsNullOrEmpty(error?.Code) ? DefaultCodeForStatus(status) : error!.Code!;
        var message = string.IsNullOrEmpty(error?.Message) ? $"Request failed with status {status}" : error!.Message!;
        var hint = error?.Hint;
        var docUrl = error?.DocUrl;
        var requestId = string.IsNullOrEmpty(envelope?.Meta?.RequestId) ? headerRequestId : envelope!.Meta!.RequestId;
        var fields = ConvertFields(error?.Fields);

        switch (status)
        {
            case 400:
                return new BadRequestException(message, status, code, hint, docUrl, fields, requestId);
            case 401:
                return new AuthenticationException(message, status, code, hint, docUrl, fields, requestId);
            case 403:
                return new PermissionDeniedException(message, status, code, hint, docUrl, fields, requestId);
            case 404:
                return new NotFoundException(message, status, code, hint, docUrl, fields, requestId);
            case 409:
                return new ConflictException(message, status, code, hint, docUrl, fields, requestId);
            case 422:
                return new ValidationException(message, status, code, hint, docUrl, fields, requestId);
            case 429:
                return new RateLimitException(
                    message, status, code, hint, docUrl, requestId,
                    retryAfter: Backoff.IntHeaderValue(headers, "Retry-After"),
                    limit: Backoff.IntHeaderValue(headers, "X-RateLimit-Limit"),
                    remaining: Backoff.IntHeaderValue(headers, "X-RateLimit-Remaining"));
            default:
                return status >= 500
                    ? new ServerException(message, status, code, hint, docUrl, fields, requestId)
                    : new NombaoneApiException(message, status, code, hint, docUrl, fields, requestId);
        }
    }

    /// <summary>The fallback error code when the error body is unusable.</summary>
    internal static string DefaultCodeForStatus(int status) => status switch
    {
        400 => NombaoneErrorCodes.ClientInvalidRequest,
        401 => NombaoneErrorCodes.ApiKeyInvalid,
        403 => NombaoneErrorCodes.ClientForbidden,
        404 => NombaoneErrorCodes.ClientResourceNotFound,
        409 => NombaoneErrorCodes.ClientConflict,
        422 => NombaoneErrorCodes.ClientValidationFailed,
        429 => NombaoneErrorCodes.RateLimitExceeded,
        502 or 503 or 504 => NombaoneErrorCodes.SystemUpstreamError,
        _ => NombaoneErrorCodes.SystemInternalError,
    };

    private static IReadOnlyDictionary<string, IReadOnlyList<string>>? ConvertFields(
        Dictionary<string, List<string>>? fields)
    {
        if (fields is null)
        {
            return null;
        }

        var converted = new Dictionary<string, IReadOnlyList<string>>(fields.Count);
        foreach (var pair in fields)
        {
            converted[pair.Key] = pair.Value;
        }

        return converted;
    }
}
