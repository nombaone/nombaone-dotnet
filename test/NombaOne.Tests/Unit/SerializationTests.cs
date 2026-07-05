using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using NombaOne;
using NombaOne.Internal;

namespace NombaOne.Tests.Unit;

public class SerializationTests
{
    private sealed class Body
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("phone")]
        public Optional<string>? Phone { get; init; }
    }

    private static string Serialize(object value) => JsonSerializer.Serialize(value, NombaoneJson.Options);

    [Fact]
    public void Unset_optional_and_null_reference_fields_are_omitted()
    {
        Assert.Equal("{\"name\":\"Ada\"}", Serialize(new Body { Name = "Ada" }));
    }

    [Fact]
    public void Explicit_null_optional_serializes_as_json_null()
    {
        Assert.Equal("{\"name\":\"Ada\",\"phone\":null}", Serialize(new Body { Name = "Ada", Phone = Optional<string>.Null }));
    }

    [Fact]
    public void Value_optional_serializes_the_value()
    {
        Assert.Equal("{\"phone\":\"+234\"}", Serialize(new Body { Phone = Optional<string>.Of("+234") }));
    }

    [Fact]
    public void Optional_supports_implicit_assignment_from_a_value()
    {
        var body = new Body { Phone = "+234" };
        Assert.Equal("{\"phone\":\"+234\"}", Serialize(body));
    }

    [Fact]
    public void Optional_round_trips_through_deserialization()
    {
        var body = JsonSerializer.Deserialize<Body>("{\"phone\":\"+234\"}", NombaoneJson.Options);
        Assert.NotNull(body!.Phone);
        Assert.False(body.Phone!.Value.IsNull);
        Assert.Equal("+234", body.Phone.Value.Value);
    }

    [Fact]
    public void QueryString_returns_empty_for_null()
    {
        Assert.Equal(string.Empty, QueryString.Build(null));
    }

    [Fact]
    public void QueryString_drops_null_values_and_encodes()
    {
        var query = new Dictionary<string, string?>
        {
            ["status"] = "open",
            ["cursor"] = null,
            ["q"] = "a b&c",
        };

        var result = QueryString.Build(query);

        Assert.StartsWith("?", result);
        Assert.Contains("status=open", result);
        Assert.DoesNotContain("cursor", result);
        Assert.Contains("q=a%20b%26c", result);
    }
}
