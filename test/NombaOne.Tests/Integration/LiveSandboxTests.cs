using System;
using System.Threading;
using System.Threading.Tasks;
using NombaOne;
using Xunit;

namespace NombaOne.Tests.Integration;

/// <summary>
/// Live integration against a real NombaOne API. Gated: set
/// <c>NOMBAONE_INTEGRATION=1</c> and <c>NOMBAONE_API_KEY</c> (and optionally
/// <c>NOMBAONE_BASE_URL</c>) to run; otherwise these are skipped.
/// </summary>
[Trait("Category", "Integration")]
public class LiveSandboxTests
{
    private static bool Enabled => Environment.GetEnvironmentVariable("NOMBAONE_INTEGRATION") == "1";

    private static Nombaone NewClient() => new(new NombaoneOptions
    {
        ApiKey = Environment.GetEnvironmentVariable("NOMBAONE_API_KEY"),
        BaseUrl = Environment.GetEnvironmentVariable("NOMBAONE_BASE_URL"),
    });

    private static string Suffix() => Guid.NewGuid().ToString("N").Substring(0, 10);

    [SkippableFact]
    public async Task Full_lifecycle_reaches_an_active_subscription()
    {
        Skip.IfNot(Enabled, "Set NOMBAONE_INTEGRATION=1 and NOMBAONE_API_KEY to run.");
        using var client = NewClient();
        var suffix = Suffix();

        var customer = await client.Customers.CreateAsync(new CustomerCreateParams
        {
            Email = $"ada-{suffix}@example.com",
            Name = "Ada Lovelace",
        });
        Assert.StartsWith("nbo", customer.Id);
        Assert.EndsWith("cus", customer.Id);
        Assert.Equal("sandbox", customer.Mode);
        Assert.False(string.IsNullOrEmpty(customer.RequestId));

        var plan = await client.Plans.CreateAsync(new PlanCreateParams { Name = $"Pro {suffix}" });
        var price = await client.Plans.Prices.CreateAsync(plan.Id, new PriceCreateParams
        {
            UnitAmountInKobo = 250_000,
            Interval = "month",
        });
        Assert.Equal(250_000, price.UnitAmountInKobo);

        var method = await client.Sandbox.CreatePaymentMethodAsync(new SandboxPaymentMethodParams { CustomerId = customer.Id });

        var subscription = await client.Subscriptions.CreateAsync(new SubscriptionCreateParams
        {
            CustomerId = customer.Id,
            PriceId = price.Id,
            PaymentMethodId = method.Id,
        });
        Assert.Contains(subscription.Status, new[] { "active", "trialing" });

        var cycle = await client.Sandbox.AdvanceCycleAsync(subscription.Id);
        Assert.StartsWith("nbo", cycle.Invoice.Id);

        var upcoming = await client.Subscriptions.RetrieveUpcomingInvoiceAsync(subscription.Id);
        Assert.Equal(subscription.Id, upcoming.SubscriptionId);

        var dunning = await client.Subscriptions.Dunning.RetrieveAsync(subscription.Id);
        Assert.False(string.IsNullOrEmpty(dunning.Status));

        var canceled = await client.Subscriptions.CancelAsync(subscription.Id);
        Assert.Contains(canceled.Status, new[] { "canceled", "active" });
    }

    [SkippableFact]
    public async Task Not_found_surfaces_a_typed_error_with_code_hint_docurl_requestid()
    {
        Skip.IfNot(Enabled, "Set NOMBAONE_INTEGRATION=1 and NOMBAONE_API_KEY to run.");
        using var client = NewClient();

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            client.Customers.RetrieveAsync("nbo000000000000cus"));

        Assert.Equal(NombaoneErrorCodes.CustomerNotFound, ex.Code);
        Assert.False(string.IsNullOrEmpty(ex.Hint));
        Assert.False(string.IsNullOrEmpty(ex.DocUrl));
        Assert.False(string.IsNullOrEmpty(ex.RequestId));
    }

    [SkippableFact]
    public async Task Idempotency_replay_returns_the_identical_resource()
    {
        Skip.IfNot(Enabled, "Set NOMBAONE_INTEGRATION=1 and NOMBAONE_API_KEY to run.");
        using var client = NewClient();
        var suffix = Suffix();
        var key = $"it-replay-{suffix}";
        var body = new CustomerCreateParams { Email = $"replay-{suffix}@example.com", Name = "Replay" };

        var first = await client.Customers.CreateAsync(body, new RequestOptions { IdempotencyKey = key });
        var second = await client.Customers.CreateAsync(body, new RequestOptions { IdempotencyKey = key });

        Assert.Equal(first.Id, second.Id);
    }

    [SkippableFact]
    public async Task Pagination_walks_real_cursors()
    {
        Skip.IfNot(Enabled, "Set NOMBAONE_INTEGRATION=1 and NOMBAONE_API_KEY to run.");
        using var client = NewClient();

        // Seed enough customers to force at least one cursor at limit=2.
        var suffix = Suffix();
        for (var i = 0; i < 3; i++)
        {
            await client.Customers.CreateAsync(new CustomerCreateParams { Email = $"page-{suffix}-{i}@example.com", Name = $"Page {i}" });
        }

        var page = await client.Customers.ListAsync(new CustomerListParams { Limit = 2 });
        Assert.True(page.Data.Count <= 2);

        var count = 0;
        await foreach (var _ in client.Customers.ListAutoPagingAsync(new CustomerListParams { Limit = 2 }))
        {
            if (++count >= 5)
            {
                break;
            }
        }

        Assert.True(count >= 3);
    }

    [SkippableFact]
    public async Task User_cancellation_propagates_without_retry()
    {
        Skip.IfNot(Enabled, "Set NOMBAONE_INTEGRATION=1 and NOMBAONE_API_KEY to run.");
        using var client = NewClient();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.Customers.ListAsync(null, null, cts.Token));
    }

    [SkippableFact]
    public async Task Escrow_read_is_reachable_or_surfaces_the_known_subaccount_state()
    {
        Skip.IfNot(Enabled, "Set NOMBAONE_INTEGRATION=1 and NOMBAONE_API_KEY to run.");
        using var client = NewClient();

        try
        {
            var escrow = await client.Settlements.RetrieveEscrowAsync();
            Assert.True(escrow.AvailableInKobo >= 0);
        }
        catch (NombaoneApiException ex)
        {
            // A sandbox org without a configured settlement subaccount returns this
            // typed error — a legitimate business state, not a client fault.
            Assert.Equal(NombaoneErrorCodes.SettlementSubaccountNotFound, ex.Code);
        }
    }
}
