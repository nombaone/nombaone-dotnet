using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NombaOne.Internal;

/// <summary>
/// The single, shared <see cref="JsonSerializerOptions"/> for the SDK. Wire
/// property names are camelCase and declared explicitly via
/// <c>[JsonPropertyName]</c> on every DTO, so no naming policy is applied.
/// Unset (null) request fields are omitted, matching the API's "send only what
/// you set" contract; the <see cref="Optional{T}"/> converter handles the few
/// fields that are cleared with an explicit JSON null.
/// </summary>
internal static class NombaoneJson
{
    /// <summary>The shared serializer options, built once.</summary>
    internal static JsonSerializerOptions Options { get; } = Build();

    private static JsonSerializerOptions Build()
    {
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = null,
            WriteIndented = false,
            // Send real UTF-8 on the wire — never over-escape '+' or the
            // non-ASCII characters common in Nigerian names and addresses. This
            // is an HTTPS API body, never embedded in HTML, so relaxed escaping
            // is both correct and safe.
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        options.Converters.Add(new OptionalJsonConverterFactory());
        return options;
    }
}
