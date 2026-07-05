using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NombaOne;
using NombaOne.Tests.Helpers;

namespace NombaOne.Tests.Unit;

public class CustomersTests
{
    private static (Nombaone Client, MockHttpHandler Handler) Wire(string dataJson = "{}", string? pagination = null)
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

    private static JsonElement Body(RecordedRequest request) => JsonDocument.Parse(request.Body!).RootElement;

    [Fact]
    public async Task Create_posts_to_customers_with_body_and_idempotency_key()
    {
        var (client, handler) = Wire();

        await client.Customers.CreateAsync(new CustomerCreateParams
        {
            Email = "ada@example.com",
            Name = "Ada Lovelace",
            Phone = "+2348012345678",
            Metadata = new Dictionary<string, object?> { ["crmId"] = "crm_812" },
        });

        var request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/v1/customers", request.Path);
        Assert.False(string.IsNullOrEmpty(request.Header("Idempotency-Key")));

        var body = Body(request);
        Assert.Equal("ada@example.com", body.GetProperty("email").GetString());
        Assert.Equal("Ada Lovelace", body.GetProperty("name").GetString());
        Assert.Equal("+2348012345678", body.GetProperty("phone").GetString());
        Assert.Equal("crm_812", body.GetProperty("metadata").GetProperty("crmId").GetString());
    }

    [Fact]
    public async Task Create_omits_unset_optional_fields()
    {
        var (client, handler) = Wire();

        await client.Customers.CreateAsync(new CustomerCreateParams { Email = "a@b.co", Name = "A" });

        var body = Body(handler.Requests.Single());
        Assert.False(body.TryGetProperty("phone", out _));
        Assert.False(body.TryGetProperty("metadata", out _));
    }

    [Fact]
    public async Task Retrieve_gets_customer_by_id()
    {
        var (client, handler) = Wire();

        await client.Customers.RetrieveAsync("nbo000000000001cus");

        var request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/v1/customers/nbo000000000001cus", request.Path);
        Assert.Null(request.Header("Idempotency-Key"));
    }

    [Fact]
    public async Task Retrieve_percent_encodes_the_id_segment()
    {
        var (client, handler) = Wire();

        await client.Customers.RetrieveAsync("weird/id space");

        Assert.Equal("/v1/customers/weird%2Fid%20space", handler.Requests.Single().Path);
    }

    [Fact]
    public async Task Update_patches_and_can_clear_phone_with_explicit_null()
    {
        var (client, handler) = Wire();

        await client.Customers.UpdateAsync("nbo1", new CustomerUpdateParams { Phone = Optional<string>.Null });

        var request = handler.Requests.Single();
        Assert.Equal("PATCH", request.Method.Method);
        Assert.Equal("/v1/customers/nbo1", request.Path);
        Assert.Equal(JsonValueKind.Null, Body(request).GetProperty("phone").ValueKind);
    }

    [Fact]
    public async Task Update_omits_phone_when_left_unset()
    {
        var (client, handler) = Wire();

        await client.Customers.UpdateAsync("nbo1", new CustomerUpdateParams { Name = "Renamed" });

        var body = Body(handler.Requests.Single());
        Assert.Equal("Renamed", body.GetProperty("name").GetString());
        Assert.False(body.TryGetProperty("phone", out _));
    }

    [Fact]
    public async Task List_gets_customers_with_filters()
    {
        var (client, handler) = Wire("[]", "{\"limit\":20,\"hasMore\":false,\"nextCursor\":null}");

        await client.Customers.ListAsync(new CustomerListParams { Email = "ada@example.com", Limit = 50 });

        var request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/v1/customers", request.Path);
        Assert.Contains("email=ada%40example.com", request.Url);
        Assert.Contains("limit=50", request.Url);
    }

    [Fact]
    public async Task ApplyDiscount_posts_coupon()
    {
        var (client, handler) = Wire();

        await client.Customers.ApplyDiscountAsync("nbo1", new CustomerApplyDiscountParams { Coupon = "LAUNCH20" });

        var request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/v1/customers/nbo1/discount", request.Path);
        Assert.Equal("LAUNCH20", Body(request).GetProperty("coupon").GetString());
    }

    [Fact]
    public async Task RemoveDiscount_deletes()
    {
        var (client, handler) = Wire();

        await client.Customers.RemoveDiscountAsync("nbo1");

        var request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal("/v1/customers/nbo1/discount", request.Path);
    }

    [Fact]
    public async Task GrantCredit_posts_integer_kobo_with_idempotency_key()
    {
        var (client, handler) = Wire();

        await client.Customers.GrantCreditAsync("nbo1", new CustomerGrantCreditParams { AmountInKobo = 250_000, Source = "goodwill" });

        var request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/v1/customers/nbo1/credit", request.Path);
        Assert.False(string.IsNullOrEmpty(request.Header("Idempotency-Key")));

        var body = Body(request);
        Assert.Equal(250_000, body.GetProperty("amountInKobo").GetInt64());
        Assert.Equal("goodwill", body.GetProperty("source").GetString());
    }

    [Fact]
    public async Task RetrieveCreditBalance_gets_credit()
    {
        var (client, handler) = Wire();

        await client.Customers.RetrieveCreditBalanceAsync("nbo1");

        var request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/v1/customers/nbo1/credit", request.Path);
    }

    [Fact]
    public async Task VoidCredit_deletes_the_grant()
    {
        var (client, handler) = Wire();

        await client.Customers.VoidCreditAsync("nbo1", "nbo000000000002crg");

        var request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal("/v1/customers/nbo1/credit/nbo000000000002crg", request.Path);
    }

    [Fact]
    public async Task Deserializes_customer_response()
    {
        const string data = "{\"domain\":\"customer\",\"id\":\"nbo000000000001cus\",\"email\":\"ada@example.com\",\"name\":\"Ada\",\"phone\":null,\"metadata\":{\"tier\":\"gold\"},\"mode\":\"sandbox\",\"createdAt\":\"2026-07-04T10:00:00.000Z\",\"updatedAt\":\"2026-07-04T10:00:00.000Z\"}";
        var (client, _) = Wire(data);

        var customer = await client.Customers.RetrieveAsync("nbo000000000001cus");

        Assert.Equal("nbo000000000001cus", customer.Id);
        Assert.Equal("ada@example.com", customer.Email);
        Assert.Null(customer.Phone);
        Assert.Equal("sandbox", customer.Mode);
        Assert.Equal("gold", customer.Metadata!["tier"].GetString());
        Assert.Equal("req_wire", customer.RequestId);
    }
}
