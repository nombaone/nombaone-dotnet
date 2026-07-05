using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NombaOne;

namespace NombaOne.Tests.Helpers;

/// <summary>Shared harness for resource wire tests: a client wired to a canned success response.</summary>
internal static class Wire
{
    public const string ListPagination = "{\"limit\":20,\"hasMore\":false,\"nextCursor\":null}";

    public static (Nombaone Client, MockHttpHandler Handler) Client(string dataJson = "{}", string? pagination = null)
    {
        var paginationBlock = pagination is null ? string.Empty : $"\"pagination\":{pagination},";
        var body = $"{{\"success\":true,\"statusCode\":200,\"data\":{dataJson},{paginationBlock}\"meta\":{{\"requestId\":\"req_wire\"}}}}";
        var handler = new MockHttpHandler((_, _, _) => Task.FromResult(Responses.Json(HttpStatusCode.OK, body)));
        var client = new Nombaone(new NombaoneOptions
        {
            ApiKey = "nbo_sandbox_test",
            BaseUrl = "https://api.test",
            HttpClient = new HttpClient(handler),
        });
        return (client, handler);
    }

    public static (Nombaone Client, MockHttpHandler Handler) ListClient() => Client("[]", ListPagination);

    public static JsonElement Body(RecordedRequest request) => JsonDocument.Parse(request.Body!).RootElement;
}
