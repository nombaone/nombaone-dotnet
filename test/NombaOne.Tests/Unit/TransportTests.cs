using System.Linq;
using System.Net;
using System.Net.Http;
using NombaOne;
using NombaOne.Internal;
using NombaOne.Tests.Helpers;

namespace NombaOne.Tests.Unit;

public class TransportTests
{
    private static RequestSpec Post(string path, object? body = null, RequestOptions? options = null) =>
        new(HttpVerbs.Post, path, body: body, options: options);

    private static RequestSpec Get(string path, RequestOptions? options = null) =>
        new(HttpVerbs.Get, path, options: options);

    private static Task<HttpResponseMessage> Ok(string? json = null) =>
        Task.FromResult(Responses.Json(HttpStatusCode.OK, json ?? Envelopes.Success()));

    [Fact]
    public async Task Success_unwraps_data_and_exposes_request_id()
    {
        var (transport, _) = TestTransport.Create((_, _, _) => Ok());

        var result = await transport.SendAsync<TestResource>(Get("/customers/nbo1"), CancellationToken.None);

        Assert.Equal("nbo000000000001cus", result.Data.Id);
        Assert.Equal(250_000, result.Data.AmountInKobo);
        Assert.Equal("req_success", result.Data.RequestId);
        Assert.Equal(200, result.Response.StatusCode);
    }

    [Fact]
    public async Task Sends_bearer_auth_accept_and_user_agent()
    {
        var (transport, handler) = TestTransport.Create((_, _, _) => Ok());

        await transport.SendAsync<TestResource>(Get("/customers/nbo1"), CancellationToken.None);

        var request = handler.Requests.Single();
        Assert.Equal("Bearer nbo_sandbox_test", request.Header("Authorization"));
        Assert.Equal("application/json", request.Header("Accept"));
        Assert.Equal("nombaone-dotnet/0.1.0", request.Header("User-Agent"));
        Assert.Equal("https://api.test/v1/customers/nbo1", request.Url);
    }

    [Fact]
    public async Task Get_carries_no_idempotency_key()
    {
        var (transport, handler) = TestTransport.Create((_, _, _) => Ok());

        await transport.SendAsync<TestResource>(Get("/customers/nbo1"), CancellationToken.None);

        Assert.Null(handler.Requests.Single().Header("Idempotency-Key"));
    }

    [Fact]
    public async Task Post_generates_an_idempotency_key()
    {
        var (transport, handler) = TestTransport.Create((_, _, _) => Ok());

        await transport.SendAsync<TestResource>(Post("/customers", new { email = "a@b.co" }), CancellationToken.None);

        Assert.False(string.IsNullOrEmpty(handler.Requests.Single().Header("Idempotency-Key")));
    }

    [Fact]
    public async Task Post_uses_the_supplied_idempotency_key()
    {
        var (transport, handler) = TestTransport.Create((_, _, _) => Ok());

        await transport.SendAsync<TestResource>(
            Post("/customers", new { email = "a@b.co" }, new RequestOptions { IdempotencyKey = "my-key" }),
            CancellationToken.None);

        Assert.Equal("my-key", handler.Requests.Single().Header("Idempotency-Key"));
    }

    // The single most important test in the SDK: an automatic retry must replay
    // the same logical operation, never mint a fresh key that could double-charge.
    [Fact]
    public async Task Post_reuses_the_same_idempotency_key_across_retries()
    {
        var (transport, handler) = TestTransport.Create((_, attempt, _) =>
            attempt < 2
                ? Task.FromResult(Responses.Json(HttpStatusCode.InternalServerError, Envelopes.Error("SYSTEM_INTERNAL_ERROR")))
                : Ok());

        var result = await transport.SendAsync<TestResource>(Post("/customers", new { email = "a@b.co" }), CancellationToken.None);

        Assert.Equal(3, handler.Requests.Count);
        var keys = handler.Requests.Select(r => r.Header("Idempotency-Key")).ToList();
        Assert.All(keys, key => Assert.False(string.IsNullOrEmpty(key)));
        Assert.Single(keys.Distinct());
        Assert.Equal("nbo000000000001cus", result.Data.Id);
    }

    [Fact]
    public async Task Retries_5xx_then_succeeds()
    {
        var (transport, handler) = TestTransport.Create((_, attempt, _) =>
            attempt == 0
                ? Task.FromResult(Responses.Json(HttpStatusCode.BadGateway, Envelopes.Error("SYSTEM_UPSTREAM_ERROR")))
                : Ok());

        await transport.SendAsync<TestResource>(Get("/customers/nbo1"), CancellationToken.None);

        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Does_not_retry_400()
    {
        var (transport, handler) = TestTransport.Create((_, _, _) =>
            Task.FromResult(Responses.Json(HttpStatusCode.BadRequest, Envelopes.Error("CLIENT_INVALID_REQUEST"))));

        await Assert.ThrowsAsync<BadRequestException>(() =>
            transport.SendAsync<TestResource>(Get("/customers/nbo1"), CancellationToken.None));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Retries_409_only_when_idempotency_in_progress()
    {
        var (transport, handler) = TestTransport.Create((_, attempt, _) =>
            attempt == 0
                ? Task.FromResult(Responses.Json(HttpStatusCode.Conflict, Envelopes.Error("IDEMPOTENCY_IN_PROGRESS")))
                : Ok());

        await transport.SendAsync<TestResource>(Post("/customers", new { email = "a@b.co" }), CancellationToken.None);

        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Does_not_retry_other_409_conflicts()
    {
        var (transport, handler) = TestTransport.Create((_, _, _) =>
            Task.FromResult(Responses.Json(HttpStatusCode.Conflict, Envelopes.Error("CUSTOMER_EMAIL_TAKEN"))));

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            transport.SendAsync<TestResource>(Post("/customers", new { email = "a@b.co" }), CancellationToken.None));

        Assert.Equal("CUSTOMER_EMAIL_TAKEN", ex.Code);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Honors_max_retries_zero()
    {
        var (transport, handler) = TestTransport.Create(
            (_, _, _) => Task.FromResult(Responses.Json(HttpStatusCode.InternalServerError, Envelopes.Error("SYSTEM_INTERNAL_ERROR"))),
            maxRetries: 0);

        await Assert.ThrowsAsync<ServerException>(() =>
            transport.SendAsync<TestResource>(Get("/customers/nbo1"), CancellationToken.None));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Non_json_error_body_degrades_to_server_error()
    {
        var (transport, _) = TestTransport.Create(
            (_, _, _) => Task.FromResult(Responses.Text(HttpStatusCode.BadGateway, "<html>502 Bad Gateway</html>")),
            maxRetries: 0);

        var ex = await Assert.ThrowsAsync<ServerException>(() =>
            transport.SendAsync<TestResource>(Get("/customers/nbo1"), CancellationToken.None));

        Assert.Equal("SYSTEM_UPSTREAM_ERROR", ex.Code);
        Assert.Equal(502, ex.StatusCode);
    }

    [Fact]
    public async Task Maps_404_with_full_error_envelope_and_folds_hint_into_message()
    {
        var (transport, _) = TestTransport.Create((_, _, _) => Task.FromResult(
            Responses.Json(HttpStatusCode.NotFound,
                Envelopes.Error("CUSTOMER_NOT_FOUND", message: "No such customer", hint: "Check the id", docUrl: "https://docs.nombaone.xyz/errors#CUSTOMER_NOT_FOUND", requestId: "req_404"))));

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            transport.SendAsync<TestResource>(Get("/customers/nbo1"), CancellationToken.None));

        Assert.Equal(404, ex.StatusCode);
        Assert.Equal("CUSTOMER_NOT_FOUND", ex.Code);
        Assert.Equal("Check the id", ex.Hint);
        Assert.Equal("https://docs.nombaone.xyz/errors#CUSTOMER_NOT_FOUND", ex.DocUrl);
        Assert.Equal("req_404", ex.RequestId);
        Assert.Contains("No such customer", ex.Message);
        Assert.Contains("Check the id", ex.Message);
    }

    [Fact]
    public async Task Maps_422_with_field_errors()
    {
        var (transport, _) = TestTransport.Create((_, _, _) => Task.FromResult(
            Responses.Json(HttpStatusCode.UnprocessableEntity,
                Envelopes.Error("CLIENT_VALIDATION_FAILED", fieldsJson: "{\"email\":[\"Invalid email\"]}"))));

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            transport.SendAsync<TestResource>(Post("/customers", new { email = "x" }), CancellationToken.None));

        Assert.NotNull(ex.Fields);
        Assert.Equal("Invalid email", ex.Fields!["email"].Single());
    }

    [Fact]
    public async Task Maps_429_with_rate_limit_headers()
    {
        var (transport, _) = TestTransport.Create(
            (_, _, _) => Task.FromResult(Responses.Json(HttpStatusCode.TooManyRequests, Envelopes.Error("RATE_LIMIT_EXCEEDED"),
                r =>
                {
                    r.Headers.TryAddWithoutValidation("Retry-After", "7");
                    r.Headers.TryAddWithoutValidation("X-RateLimit-Limit", "100");
                    r.Headers.TryAddWithoutValidation("X-RateLimit-Remaining", "0");
                })),
            maxRetries: 0);

        var ex = await Assert.ThrowsAsync<RateLimitException>(() =>
            transport.SendAsync<TestResource>(Get("/customers/nbo1"), CancellationToken.None));

        Assert.Equal(7, ex.RetryAfter);
        Assert.Equal(100, ex.Limit);
        Assert.Equal(0, ex.Remaining);
    }

    [Fact]
    public async Task Request_id_falls_back_to_header_when_meta_absent()
    {
        const string json = "{\"success\":true,\"statusCode\":200,\"data\":{\"id\":\"nbo1\",\"amountInKobo\":1},\"meta\":{}}";
        var (transport, _) = TestTransport.Create((_, _, _) => Task.FromResult(
            Responses.Json(HttpStatusCode.OK, json, r => r.Headers.TryAddWithoutValidation("X-Request-Id", "req_from_header"))));

        var result = await transport.SendAsync<TestResource>(Get("/customers/nbo1"), CancellationToken.None);

        Assert.Equal("req_from_header", result.Data.RequestId);
    }

    [Fact]
    public async Task Timeout_is_retried_then_surfaced_as_timeout_exception()
    {
        var (transport, handler) = TestTransport.Create(
            async (_, _, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
                return Responses.Json(HttpStatusCode.OK, Envelopes.Success());
            },
            maxRetries: 1,
            timeout: TimeSpan.FromMilliseconds(40));

        await Assert.ThrowsAsync<NombaoneTimeoutException>(() =>
            transport.SendAsync<TestResource>(Get("/customers/nbo1"), CancellationToken.None));

        Assert.Equal(2, handler.Requests.Count); // original + 1 retry
    }

    [Fact]
    public async Task User_cancellation_is_not_retried_and_propagates()
    {
        using var cts = new CancellationTokenSource();
        var (transport, handler) = TestTransport.Create(
            async (_, _, ct) =>
            {
                cts.Cancel();
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
                return Responses.Json(HttpStatusCode.OK, Envelopes.Success());
            },
            maxRetries: 3);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            transport.SendAsync<TestResource>(Get("/customers/nbo1"), cts.Token));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Merges_default_and_per_call_headers_and_null_removes()
    {
        var (transport, handler) = TestTransport.Create(
            (_, _, _) => Ok(),
            defaultHeaders: new Dictionary<string, string> { ["X-App"] = "default", ["X-Keep"] = "yes" });

        await transport.SendAsync<TestResource>(
            Get("/customers/nbo1", new RequestOptions
            {
                Headers = new Dictionary<string, string?> { ["X-App"] = "override", ["X-Keep"] = null },
            }),
            CancellationToken.None);

        var request = handler.Requests.Single();
        Assert.Equal("override", request.Header("X-App"));
        Assert.Null(request.Header("X-Keep"));
    }

    [Fact]
    public async Task List_response_populates_pagination()
    {
        var (transport, _) = TestTransport.Create((_, _, _) =>
            Task.FromResult(Responses.Json(HttpStatusCode.OK, Envelopes.SuccessList(hasMore: true, nextCursor: "cur_2", limit: 50))));

        var result = await transport.SendAsync<List<TestResource>>(Get("/customers"), CancellationToken.None);

        Assert.NotNull(result.Pagination);
        Assert.Equal(50, result.Pagination!.Limit);
        Assert.True(result.Pagination.HasMore);
        Assert.Equal("cur_2", result.Pagination.NextCursor);
        Assert.Single(result.Data);
    }
}
