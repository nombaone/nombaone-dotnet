using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NombaOne;
using NombaOne.Tests.Helpers;

namespace NombaOne.Tests.Unit;

public class MoneyPlatformTests
{
    private static string[] Routes(MockHttpHandler handler) =>
        handler.Requests.Select(r => $"{r.Method.Method} {r.Path}").ToArray();

    // ---- Payment methods ----

    [Fact]
    public async Task PaymentMethods_single_methods()
    {
        var (client, handler) = Wire.Client();

        await client.PaymentMethods.SetupAsync(new PaymentMethodSetupParams { CustomerRef = "cus1", AmountInKobo = 5_000, CallbackUrl = "https://x.co" });
        await client.PaymentMethods.CreateVirtualAccountAsync(new PaymentMethodVirtualAccountParams { CustomerRef = "cus1" });
        await client.PaymentMethods.RetrieveAsync("pmt1");
        await client.PaymentMethods.SetDefaultAsync("pmt1");
        await client.PaymentMethods.RemoveAsync("pmt1");

        Assert.Equal(new[]
        {
            "POST /v1/payment-methods/setup",
            "POST /v1/payment-methods/virtual-account",
            "GET /v1/payment-methods/pmt1",
            "POST /v1/payment-methods/pmt1/default",
            "DELETE /v1/payment-methods/pmt1",
        }, Routes(handler));
        Assert.Equal(5_000, Wire.Body(handler.Requests[0]).GetProperty("amountInKobo").GetInt64());
    }

    [Fact]
    public async Task PaymentMethods_list_uses_customerRef_filter()
    {
        var (client, handler) = Wire.ListClient();
        await client.PaymentMethods.ListAsync(new PaymentMethodListParams { CustomerRef = "cus1" });
        Assert.Equal("GET /v1/payment-methods", Routes(handler).Single());
        Assert.Contains("customerRef=cus1", handler.Requests[0].Url);
    }

    // ---- Mandates ----

    [Fact]
    public async Task Mandates_create_and_retrieve()
    {
        var (client, handler) = Wire.Client();

        await client.Mandates.CreateAsync(new MandateCreateParams
        {
            CustomerRef = "cus1",
            CustomerAccountNumber = "0123456789",
            BankCode = "058",
            CustomerName = "Ada",
            CustomerAccountName = "Ada",
            CustomerPhoneNumber = "+234",
            CustomerAddress = "Lagos",
            Narration = "sub",
            MaxAmountInKobo = 500_000,
        });
        await client.Mandates.RetrieveAsync("pmt1");

        Assert.Equal(new[] { "POST /v1/mandates", "GET /v1/mandates/pmt1" }, Routes(handler));
        Assert.False(string.IsNullOrEmpty(handler.Requests[0].Header("Idempotency-Key")));
        Assert.Equal(500_000, Wire.Body(handler.Requests[0]).GetProperty("maxAmountInKobo").GetInt64());
    }

    // ---- Settlements ----

    [Fact]
    public async Task Settlements_methods_and_payout_uses_stable_idempotency_key()
    {
        var (client, handler) = Wire.Client();

        await client.Settlements.RetrieveAsync("stl1");
        await client.Settlements.RetrieveEscrowAsync();
        await client.Settlements.RefundAsync("stl1", new SettlementRefundParams { AmountInKobo = 100_000 });
        await client.Settlements.CreatePayoutAsync(
            new PayoutCreateParams { AmountInKobo = 5_000_000, BankCode = "058", AccountNumber = "0123456789" },
            new RequestOptions { IdempotencyKey = "payout-42" });

        Assert.Equal(new[]
        {
            "GET /v1/settlements/stl1",
            "GET /v1/settlements/escrow",
            "POST /v1/settlements/stl1/refund",
            "POST /v1/settlements/payout",
        }, Routes(handler));
        Assert.Equal("payout-42", handler.Requests[3].Header("Idempotency-Key"));
        Assert.Equal(100_000, Wire.Body(handler.Requests[2]).GetProperty("amountInKobo").GetInt64());
    }

    [Fact]
    public async Task Settlements_list()
    {
        var (client, handler) = Wire.ListClient();
        await client.Settlements.ListAsync(new SettlementListParams { Status = "settled" });
        Assert.Equal("GET /v1/settlements", Routes(handler).Single());
        Assert.Contains("status=settled", handler.Requests[0].Url);
    }

    // ---- Webhook endpoints (+ deliveries) ----

    [Fact]
    public async Task WebhookEndpoints_single_methods()
    {
        var (client, handler) = Wire.Client();

        await client.WebhookEndpoints.CreateAsync(new WebhookEndpointCreateParams { Url = "https://x.co/h" });
        await client.WebhookEndpoints.RetrieveAsync("whk1");
        await client.WebhookEndpoints.UpdateAsync("whk1", new WebhookEndpointUpdateParams { Disabled = true });
        await client.WebhookEndpoints.DeleteAsync("whk1");
        await client.WebhookEndpoints.RotateSecretAsync("whk1");
        await client.WebhookEndpoints.Deliveries.RetrieveAsync("whk1", "whd1");
        await client.WebhookEndpoints.Deliveries.ReplayAsync("whk1", "whd1");

        Assert.Equal(new[]
        {
            "POST /v1/webhooks",
            "GET /v1/webhooks/whk1",
            "PATCH /v1/webhooks/whk1",
            "DELETE /v1/webhooks/whk1",
            "POST /v1/webhooks/whk1/rotate-secret",
            "GET /v1/webhooks/whk1/deliveries/whd1",
            "POST /v1/webhooks/whk1/deliveries/whd1/replay",
        }, Routes(handler));
    }

    [Fact]
    public async Task WebhookEndpoints_list_and_deliveries_list()
    {
        var (client, handler) = Wire.ListClient();
        await client.WebhookEndpoints.ListAsync();
        await client.WebhookEndpoints.Deliveries.ListAsync("whk1", new WebhookDeliveryListParams { Status = "dead" });

        Assert.Equal(new[] { "GET /v1/webhooks", "GET /v1/webhooks/whk1/deliveries" }, Routes(handler));
        Assert.Contains("status=dead", handler.Requests[1].Url);
    }

    [Fact]
    public async Task WebhookEndpoint_create_exposes_signing_secret_once()
    {
        var (client, _) = Wire.Client("{\"domain\":\"webhook\",\"id\":\"nbo1whk\",\"url\":\"https://x.co\",\"enabledEvents\":[\"*\"],\"signingSecretPrefix\":\"whsec_ab\",\"signingSecret\":\"whsec_abcdef123\",\"disabledAt\":null,\"createdAt\":\"2026-07-01T00:00:00.000Z\"}");

        var endpoint = await client.WebhookEndpoints.CreateAsync(new WebhookEndpointCreateParams { Url = "https://x.co" });

        Assert.Equal("whsec_abcdef123", endpoint.SigningSecret);
        Assert.Equal("whsec_ab", endpoint.SigningSecretPrefix);
    }

    // ---- Events ----

    [Fact]
    public async Task Events_methods()
    {
        var (single, singleHandler) = Wire.Client();
        await single.Events.RetrieveAsync("evt1");
        Assert.Equal("GET /v1/events/evt1", Routes(singleHandler).Single());

        var (list, listHandler) = Wire.ListClient();
        await list.Events.ListAsync(new EventListParams { Type = "invoice.paid" });
        Assert.Equal("GET /v1/events", Routes(listHandler).Single());
        Assert.Contains("type=invoice.paid", listHandler.Requests[0].Url);
    }

    [Fact]
    public async Task Events_catalog_returns_a_typed_map()
    {
        var (client, handler) = Wire.Client("{\"invoice.paid\":{\"when\":\"Invoice paid\",\"payload\":[\"reference\"]}}");

        var catalog = await client.Events.CatalogAsync();

        Assert.Equal("GET /v1/events/catalog", Routes(handler).Single());
        Assert.Equal("Invoice paid", catalog["invoice.paid"].When);
        Assert.Equal("reference", catalog["invoice.paid"].Payload.Single());
    }

    // ---- Organization (+ billing) ----

    [Fact]
    public async Task Organization_uses_put_for_updates()
    {
        var (client, handler) = Wire.Client();

        await client.Organization.RetrieveAsync();
        await client.Organization.UpdateAsync(new TenantSettingsUpdateParams { SettlementMode = "collect_then_payout" });
        await client.Organization.Billing.RetrieveAsync();
        await client.Organization.Billing.UpdateAsync(new BillingSettingsUpdateParams { CommsEnabled = true });

        Assert.Equal(new[]
        {
            "GET /v1/organization",
            "PUT /v1/organization",
            "GET /v1/organization/billing",
            "PUT /v1/organization/billing",
        }, Routes(handler));
        Assert.Equal("collect_then_payout", Wire.Body(handler.Requests[1]).GetProperty("settlementMode").GetString());
        Assert.True(Wire.Body(handler.Requests[3]).GetProperty("commsEnabled").GetBoolean());
    }

    // ---- Metrics ----

    [Fact]
    public async Task Metrics_billing_passes_window()
    {
        var (client, handler) = Wire.Client();
        await client.Metrics.BillingAsync(new BillingMetricsParams { From = "2026-01-01T00:00:00Z" });
        Assert.Equal("GET /v1/metrics/billing", Routes(handler).Single());
        Assert.Contains("from=2026-01-01", handler.Requests[0].Url);
    }

    // ---- Sandbox ----

    [Fact]
    public async Task Sandbox_methods_hit_the_right_routes_with_sandbox_key()
    {
        var (client, handler) = Wire.Client();

        await client.Sandbox.CreatePaymentMethodAsync(new SandboxPaymentMethodParams { CustomerId = "cus1", Behavior = "decline_insufficient_funds" });
        await client.Sandbox.AdvanceCycleAsync("sub1");
        await client.Sandbox.SimulateWebhookAsync(new SandboxSimulateWebhookParams { Type = "invoice.payment_failed" });

        Assert.Equal(new[]
        {
            "POST /v1/sandbox/payment-methods",
            "POST /v1/sandbox/subscriptions/sub1/advance-cycle",
            "POST /v1/sandbox/webhooks/simulate",
        }, Routes(handler));
    }

    [Fact]
    public void Sandbox_throws_locally_with_a_live_key_before_any_network_call()
    {
        var handler = new MockHttpHandler((_, _, _) => Task.FromResult(Responses.Json(System.Net.HttpStatusCode.OK, "{}")));
        using var client = new Nombaone(new NombaoneOptions
        {
            ApiKey = "nbo_live_secret",
            HttpClient = new HttpClient(handler),
        });

        // The guard throws synchronously, before the Task is ever created — the
        // "fail locally, before any network call" contract. An Action lambda
        // (not Func<Task>) is what asserts that synchronous throw.
        var ex = Assert.Throws<NombaoneException>(() =>
        {
            _ = client.Sandbox.CreatePaymentMethodAsync(new SandboxPaymentMethodParams { CustomerId = "cus1" });
        });

        Assert.Contains("nbo_sandbox_", ex.Message);
        Assert.Empty(handler.Requests); // never touched the network
    }
}
