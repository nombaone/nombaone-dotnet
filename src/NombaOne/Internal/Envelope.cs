using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NombaOne.Internal;

/// <summary>Wire shape of a success envelope: <c>{ success, statusCode, data, pagination?, meta }</c>.</summary>
/// <typeparam name="T">The unwrapped <c>data</c> type.</typeparam>
internal sealed class SuccessEnvelope<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("statusCode")]
    public int StatusCode { get; init; }

    [JsonPropertyName("data")]
    public T? Data { get; init; }

    [JsonPropertyName("pagination")]
    public PaginationDto? Pagination { get; init; }

    [JsonPropertyName("meta")]
    public MetaDto? Meta { get; init; }
}

/// <summary>The <c>meta</c> block present on every response.</summary>
internal sealed class MetaDto
{
    [JsonPropertyName("requestId")]
    public string? RequestId { get; init; }
}

/// <summary>The top-level <c>pagination</c> block on list responses.</summary>
internal sealed class PaginationDto
{
    [JsonPropertyName("limit")]
    public int Limit { get; init; }

    [JsonPropertyName("hasMore")]
    public bool HasMore { get; init; }

    [JsonPropertyName("nextCursor")]
    public string? NextCursor { get; init; }
}

/// <summary>Wire shape of an error envelope.</summary>
internal sealed class ErrorEnvelope
{
    [JsonPropertyName("error")]
    public ErrorBody? Error { get; init; }

    [JsonPropertyName("meta")]
    public MetaDto? Meta { get; init; }
}

/// <summary>The <c>error</c> object inside an error envelope.</summary>
internal sealed class ErrorBody
{
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("hint")]
    public string? Hint { get; init; }

    [JsonPropertyName("docUrl")]
    public string? DocUrl { get; init; }

    [JsonPropertyName("fields")]
    public Dictionary<string, List<string>>? Fields { get; init; }
}
