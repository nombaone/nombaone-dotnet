using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NombaOne.Internal;

/// <summary>
/// Serializes <see cref="Optional{T}"/> to either its wrapped value or an
/// explicit JSON <c>null</c>. The "unset" state is modelled one level up, as a
/// <c>null</c> <c>Optional&lt;T&gt;?</c> property omitted via
/// <see cref="JsonIgnoreCondition.WhenWritingNull"/>.
/// </summary>
internal sealed class OptionalJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType &&
        typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(OptionalJsonConverter<>).MakeGenericType(valueType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

/// <summary>Per-<typeparamref name="T"/> converter for <see cref="Optional{T}"/>.</summary>
/// <typeparam name="T">The wrapped value type.</typeparam>
internal sealed class OptionalJsonConverter<T> : JsonConverter<Optional<T>>
{
    public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return Optional<T>.Null;
        }

        var value = JsonSerializer.Deserialize<T>(ref reader, options)!;
        return Optional<T>.Of(value);
    }

    public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options)
    {
        if (value.IsNull)
        {
            writer.WriteNullValue();
        }
        else
        {
            JsonSerializer.Serialize(writer, value.Value, options);
        }
    }
}
