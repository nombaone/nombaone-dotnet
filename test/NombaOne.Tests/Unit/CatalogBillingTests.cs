using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NombaOne;
using NombaOne.Tests.Helpers;

namespace NombaOne.Tests.Unit;

public class CatalogBillingTests
{
    private static string[] Routes(MockHttpHandler handler) =>
        handler.Requests.Select(r => $"{r.Method.Method} {r.Path}").ToArray();

    // ---- Plans (+ nested prices) ----

    [Fact]
    public async Task Plans_single_methods_hit_the_right_routes()
    {
        var (client, handler) = Wire.Client();

        await client.Plans.CreateAsync(new PlanCreateParams { Name = "Pro" });
        await client.Plans.RetrieveAsync("pln1");
        await client.Plans.UpdateAsync("pln1", new PlanUpdateParams { Description = Optional<string>.Null });
        await client.Plans.ArchiveAsync("pln1");
        await client.Plans.Prices.CreateAsync("pln1", new PriceCreateParams { UnitAmountInKobo = 250_000, Interval = "month" });

        Assert.Equal(new[]
        {
            "POST /v1/plans",
            "GET /v1/plans/pln1",
            "PATCH /v1/plans/pln1",
            "POST /v1/plans/pln1/archive",
            "POST /v1/plans/pln1/prices",
        }, Routes(handler));

        Assert.Equal("Pro", Wire.Body(handler.Requests[0]).GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, Wire.Body(handler.Requests[2]).GetProperty("description").ValueKind);
        Assert.Equal("{}", handler.Requests[3].Body); // archive sends an empty body
        Assert.Equal(250_000, Wire.Body(handler.Requests[4]).GetProperty("unitAmountInKobo").GetInt64());
    }

    [Fact]
    public async Task Plans_list_methods()
    {
        var (client, handler) = Wire.ListClient();

        await client.Plans.ListAsync(new PlanListParams { Status = "active", Limit = 50 });
        await client.Plans.Prices.ListAsync("pln1");

        Assert.Equal("GET /v1/plans", Routes(handler)[0]);
        Assert.Contains("status=active", handler.Requests[0].Url);
        Assert.Contains("limit=50", handler.Requests[0].Url);
        Assert.Equal("GET /v1/plans/pln1/prices", Routes(handler)[1]);
    }

    // ---- Prices ----

    [Fact]
    public async Task Prices_single_methods()
    {
        var (client, handler) = Wire.Client();

        await client.Prices.RetrieveAsync("prc1");
        await client.Prices.DeactivateAsync("prc1");

        Assert.Equal(new[] { "GET /v1/prices/prc1", "POST /v1/prices/prc1/deactivate" }, Routes(handler));
        Assert.Equal("{}", handler.Requests[1].Body);
    }

    [Fact]
    public async Task Prices_list_uses_planRef_and_active_filters()
    {
        var (client, handler) = Wire.ListClient();

        await client.Prices.ListAsync(new PriceListParams { PlanRef = "pln1", Active = true });

        Assert.Equal("GET /v1/prices", Routes(handler).Single());
        Assert.Contains("planRef=pln1", handler.Requests[0].Url);
        Assert.Contains("active=true", handler.Requests[0].Url);
    }

    // ---- Subscriptions (+ schedule + dunning) ----

    [Fact]
    public async Task Subscriptions_single_methods_hit_the_right_routes()
    {
        var (client, handler) = Wire.Client();

        await client.Subscriptions.CreateAsync(new SubscriptionCreateParams { CustomerId = "cus1", PriceId = "prc1" });
        await client.Subscriptions.RetrieveAsync("sub1");
        await client.Subscriptions.UpdateAsync("sub1", new SubscriptionUpdateParams { DefaultPaymentMethodId = "pmt1" });
        await client.Subscriptions.PauseAsync("sub1", new SubscriptionPauseParams { MaxDays = 7 });
        await client.Subscriptions.ResumeAsync("sub1");
        await client.Subscriptions.CancelAsync("sub1", new SubscriptionCancelParams { Mode = "at_period_end" });
        await client.Subscriptions.ResubscribeAsync("sub1");
        await client.Subscriptions.ChangeAsync("sub1", new SubscriptionChangeParams { PriceId = "prc2" });
        await client.Subscriptions.UpdatePaymentMethodAsync("sub1", new SubscriptionUpdatePaymentMethodParams { CheckoutToken = "tok" });
        await client.Subscriptions.RetrieveUpcomingInvoiceAsync("sub1");
        await client.Subscriptions.ApplyDiscountAsync("sub1", new SubscriptionApplyDiscountParams { Coupon = "LAUNCH20" });
        await client.Subscriptions.RemoveDiscountAsync("sub1");
        await client.Subscriptions.Schedule.CreateAsync("sub1", new SubscriptionScheduleCreateParams { PriceId = "prc2" });
        await client.Subscriptions.Schedule.RetrieveAsync("sub1");
        await client.Subscriptions.Schedule.ReleaseAsync("sub1");
        await client.Subscriptions.Dunning.RetrieveAsync("sub1");

        Assert.Equal(new[]
        {
            "POST /v1/subscriptions",
            "GET /v1/subscriptions/sub1",
            "PATCH /v1/subscriptions/sub1",
            "POST /v1/subscriptions/sub1/pause",
            "POST /v1/subscriptions/sub1/resume",
            "POST /v1/subscriptions/sub1/cancel",
            "POST /v1/subscriptions/sub1/resubscribe",
            "POST /v1/subscriptions/sub1/change",
            "POST /v1/subscriptions/sub1/payment-method",
            "GET /v1/subscriptions/sub1/upcoming-invoice",
            "POST /v1/subscriptions/sub1/discount",
            "DELETE /v1/subscriptions/sub1/discount",
            "POST /v1/subscriptions/sub1/schedule",
            "GET /v1/subscriptions/sub1/schedule",
            "DELETE /v1/subscriptions/sub1/schedule",
            "GET /v1/subscriptions/sub1/dunning",
        }, Routes(handler));

        var create = Wire.Body(handler.Requests[0]);
        Assert.Equal("cus1", create.GetProperty("customerId").GetString());
        Assert.Equal("prc1", create.GetProperty("priceId").GetString());
        Assert.Equal("at_period_end", Wire.Body(handler.Requests[5]).GetProperty("mode").GetString());
        Assert.Equal("prc2", Wire.Body(handler.Requests[7]).GetProperty("priceId").GetString());

        // Every money-moving POST carries an idempotency key.
        foreach (var index in new[] { 0, 5, 6, 7 })
        {
            Assert.False(string.IsNullOrEmpty(handler.Requests[index].Header("Idempotency-Key")));
        }
    }

    [Fact]
    public async Task Subscriptions_list_methods()
    {
        var (client, handler) = Wire.ListClient();

        await client.Subscriptions.ListAsync(new SubscriptionListParams { CustomerId = "cus1", Status = "active" });
        await client.Subscriptions.ListEventsAsync("sub1");
        await client.Subscriptions.Dunning.ListAttemptsAsync("sub1");

        Assert.Equal(new[]
        {
            "GET /v1/subscriptions",
            "GET /v1/subscriptions/sub1/events",
            "GET /v1/subscriptions/sub1/dunning/attempts",
        }, Routes(handler));
        Assert.Contains("customerId=cus1", handler.Requests[0].Url);
        Assert.Contains("status=active", handler.Requests[0].Url);
    }

    // ---- Invoices ----

    [Fact]
    public async Task Invoices_methods()
    {
        var (single, singleHandler) = Wire.Client();
        await single.Invoices.RetrieveAsync("inv1");
        await single.Invoices.VoidAsync("inv1", new InvoiceVoidParams { Comment = "duplicate" });
        await single.Invoices.VoidAsync("inv2");

        Assert.Equal(new[] { "GET /v1/invoices/inv1", "POST /v1/invoices/inv1/void", "POST /v1/invoices/inv2/void" }, Routes(singleHandler));
        Assert.Equal("duplicate", Wire.Body(singleHandler.Requests[1]).GetProperty("comment").GetString());
        Assert.Equal("{}", singleHandler.Requests[2].Body);

        var (list, listHandler) = Wire.ListClient();
        await list.Invoices.ListAsync(new InvoiceListParams { Status = "open", CustomerId = "cus1" });
        Assert.Equal("GET /v1/invoices", Routes(listHandler).Single());
        Assert.Contains("status=open", listHandler.Requests[0].Url);
        Assert.Contains("customerId=cus1", listHandler.Requests[0].Url);
    }

    // ---- Coupons ----

    [Fact]
    public async Task Coupons_methods()
    {
        var (single, singleHandler) = Wire.Client();
        await single.Coupons.CreateAsync(new CouponCreateParams { Code = "LAUNCH20", PercentOff = 20, Duration = "repeating", DurationInCycles = 3 });
        await single.Coupons.RetrieveAsync("cpn1");
        await single.Coupons.UpdateAsync("cpn1", new CouponUpdateParams { MaxRedemptions = 100 });

        Assert.Equal(new[] { "POST /v1/coupons", "GET /v1/coupons/cpn1", "PATCH /v1/coupons/cpn1" }, Routes(singleHandler));
        var body = Wire.Body(singleHandler.Requests[0]);
        Assert.Equal("LAUNCH20", body.GetProperty("code").GetString());
        Assert.Equal(20, body.GetProperty("percentOff").GetInt32());
        Assert.Equal("repeating", body.GetProperty("duration").GetString());

        var (list, listHandler) = Wire.ListClient();
        await list.Coupons.ListAsync();
        Assert.Equal("GET /v1/coupons", Routes(listHandler).Single());
    }

    // ---- Deserialization ----

    [Fact]
    public async Task Deserializes_subscription_response()
    {
        const string data = "{\"domain\":\"subscription\",\"id\":\"nbo000000000001sub\",\"customerId\":\"cus1\",\"priceId\":\"prc1\"," +
            "\"status\":\"active\",\"collectionMethod\":\"charge_automatically\",\"currentPeriodIndex\":2," +
            "\"currentPeriodStart\":\"2026-07-01T00:00:00.000Z\",\"currentPeriodEnd\":\"2026-08-01T00:00:00.000Z\"," +
            "\"trialStart\":null,\"trialEnd\":null,\"cancelAtPeriodEnd\":false,\"canceledAt\":null,\"endedAt\":null," +
            "\"cancellationReason\":null,\"defaultPaymentMethodId\":\"pmt1\"," +
            "\"items\":[{\"id\":\"it1\",\"priceId\":\"prc1\",\"quantity\":2}],\"latestInvoiceId\":\"inv1\"," +
            "\"currency\":\"NGN\",\"mode\":\"sandbox\",\"createdAt\":\"2026-06-01T00:00:00.000Z\"}";
        var (client, _) = Wire.Client(data);

        var subscription = await client.Subscriptions.RetrieveAsync("nbo000000000001sub");

        Assert.Equal("active", subscription.Status);
        Assert.Equal(2, subscription.CurrentPeriodIndex);
        Assert.Null(subscription.CancellationReason);
        Assert.Single(subscription.Items);
        Assert.Equal("prc1", subscription.Items[0].PriceId);
        Assert.Equal(2, subscription.Items[0].Quantity);
        Assert.Equal(2026, subscription.CurrentPeriodStart!.Value.Year);
    }

    [Fact]
    public async Task Deserializes_price_response_with_long_kobo()
    {
        const string data = "{\"domain\":\"price\",\"id\":\"nbo000000000001prc\",\"planId\":\"pln1\",\"unitAmountInKobo\":250000," +
            "\"currency\":\"NGN\",\"interval\":\"month\",\"intervalCount\":1,\"usageType\":\"licensed\"," +
            "\"billingScheme\":\"per_unit\",\"trialPeriodDays\":0,\"active\":true,\"metadata\":{},\"mode\":\"sandbox\",\"createdAt\":\"2026-06-01T00:00:00.000Z\"}";
        var (client, _) = Wire.Client(data);

        var price = await client.Prices.RetrieveAsync("nbo000000000001prc");

        Assert.Equal(250_000L, price.UnitAmountInKobo);
        Assert.Equal("month", price.Interval);
        Assert.True(price.Active);
    }
}
