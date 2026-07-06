using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NombaOne;
using NombaOne.Tests.Helpers;

namespace NombaOne.Tests.Conformance;

/// <summary>
/// The drift alarm. Every SDK method is exercised against a recording transport;
/// each emitted <c>METHOD /v1/path</c> must exist in the committed OpenAPI
/// snapshot, and every spec operation (minus the explicit exclusions) must be
/// emitted by some SDK method. Either direction failing names the route.
/// </summary>
public class ConformanceTests
{
    private sealed record SpecOp(string Method, string[] Segments, string Key);

    private static readonly HashSet<string> HttpMethods = new() { "get", "post", "patch", "put", "delete" };

    // Routes intentionally NOT in the SDK surface.
    private static readonly HashSet<string> Excluded = new()
    {
        "get /v1/health",
        "get /v1/openapi.json",
        "post /v1/examples",
        "get /v1/examples",
        "get /v1/examples/{id}",
    };

    private const string Id = "nbo000000000001xxx";
    private const string Grant = "nbo000000000002crg";
    private const string Delivery = "nbo000000000003whd";

    private static readonly Func<Nombaone, Task>[] Exercises =
    {
        // customers
        c => c.Customers.CreateAsync(new CustomerCreateParams { Email = "a@b.co", Name = "A" }),
        c => c.Customers.RetrieveAsync(Id),
        c => c.Customers.UpdateAsync(Id, new CustomerUpdateParams { Name = "B" }),
        c => c.Customers.ListAsync(),
        c => c.Customers.ApplyDiscountAsync(Id, new CustomerApplyDiscountParams { Coupon = "X" }),
        c => c.Customers.RemoveDiscountAsync(Id),
        c => c.Customers.GrantCreditAsync(Id, new CustomerGrantCreditParams { AmountInKobo = 100 }),
        c => c.Customers.RetrieveCreditBalanceAsync(Id),
        c => c.Customers.VoidCreditAsync(Id, Grant),
        // plans (+ nested prices)
        c => c.Plans.CreateAsync(new PlanCreateParams { Name = "Pro" }),
        c => c.Plans.RetrieveAsync(Id),
        c => c.Plans.UpdateAsync(Id, new PlanUpdateParams { Name = "Pro2" }),
        c => c.Plans.ListAsync(),
        c => c.Plans.ArchiveAsync(Id),
        c => c.Plans.Prices.CreateAsync(Id, new PriceCreateParams { UnitAmountInKobo = 100, Interval = "month" }),
        c => c.Plans.Prices.ListAsync(Id),
        // prices
        c => c.Prices.RetrieveAsync(Id),
        c => c.Prices.ListAsync(),
        c => c.Prices.DeactivateAsync(Id),
        // subscriptions (+ schedule + dunning)
        c => c.Subscriptions.CreateAsync(new SubscriptionCreateParams { CustomerId = Id, PriceId = Id, PaymentMethodId = Id }),
        c => c.Subscriptions.RetrieveAsync(Id),
        c => c.Subscriptions.UpdateAsync(Id, new SubscriptionUpdateParams { DefaultPaymentMethodId = Id }),
        c => c.Subscriptions.ListAsync(),
        c => c.Subscriptions.ListEventsAsync(Id),
        c => c.Subscriptions.PauseAsync(Id),
        c => c.Subscriptions.ResumeAsync(Id),
        c => c.Subscriptions.CancelAsync(Id),
        c => c.Subscriptions.ResubscribeAsync(Id),
        c => c.Subscriptions.ChangeAsync(Id, new SubscriptionChangeParams { PriceId = Id }),
        c => c.Subscriptions.UpdatePaymentMethodAsync(Id, new SubscriptionUpdatePaymentMethodParams { CheckoutToken = "t" }),
        c => c.Subscriptions.RetrieveUpcomingInvoiceAsync(Id),
        c => c.Subscriptions.ApplyDiscountAsync(Id, new SubscriptionApplyDiscountParams { Coupon = "X" }),
        c => c.Subscriptions.RemoveDiscountAsync(Id),
        c => c.Subscriptions.Schedule.CreateAsync(Id, new SubscriptionScheduleCreateParams { PriceId = Id }),
        c => c.Subscriptions.Schedule.RetrieveAsync(Id),
        c => c.Subscriptions.Schedule.ReleaseAsync(Id),
        c => c.Subscriptions.Dunning.RetrieveAsync(Id),
        c => c.Subscriptions.Dunning.ListAttemptsAsync(Id),
        // invoices
        c => c.Invoices.RetrieveAsync(Id),
        c => c.Invoices.ListAsync(),
        c => c.Invoices.VoidAsync(Id),
        // coupons
        c => c.Coupons.CreateAsync(new CouponCreateParams { Code = "X", PercentOff = 10, Duration = "once" }),
        c => c.Coupons.RetrieveAsync(Id),
        c => c.Coupons.UpdateAsync(Id, new CouponUpdateParams { MaxRedemptions = 5 }),
        c => c.Coupons.ListAsync(),
        // payment methods
        c => c.PaymentMethods.SetupAsync(new PaymentMethodSetupParams { CustomerRef = Id, AmountInKobo = 100, CallbackUrl = "https://x.co" }),
        c => c.PaymentMethods.CreateVirtualAccountAsync(new PaymentMethodVirtualAccountParams { CustomerRef = Id }),
        c => c.PaymentMethods.RetrieveAsync(Id),
        c => c.PaymentMethods.ListAsync(),
        c => c.PaymentMethods.SetDefaultAsync(Id),
        c => c.PaymentMethods.RemoveAsync(Id),
        // mandates
        c => c.Mandates.CreateAsync(new MandateCreateParams
        {
            CustomerRef = Id,
            CustomerAccountNumber = "0123456789",
            BankCode = "058",
            CustomerName = "A",
            CustomerAccountName = "A",
            CustomerPhoneNumber = "+234",
            CustomerAddress = "Lagos",
            Narration = "sub",
            MaxAmountInKobo = 100,
        }),
        c => c.Mandates.RetrieveAsync(Id),
        // settlements
        c => c.Settlements.RetrieveAsync(Id),
        c => c.Settlements.ListAsync(),
        c => c.Settlements.RetrieveEscrowAsync(),
        c => c.Settlements.RefundAsync(Id),
        c => c.Settlements.CreatePayoutAsync(new PayoutCreateParams { AmountInKobo = 100, BankCode = "058", AccountNumber = "01" }),
        // webhook endpoints (+ deliveries)
        c => c.WebhookEndpoints.CreateAsync(new WebhookEndpointCreateParams { Url = "https://x.co/h" }),
        c => c.WebhookEndpoints.RetrieveAsync(Id),
        c => c.WebhookEndpoints.UpdateAsync(Id, new WebhookEndpointUpdateParams { Disabled = true }),
        c => c.WebhookEndpoints.ListAsync(),
        c => c.WebhookEndpoints.DeleteAsync(Id),
        c => c.WebhookEndpoints.RotateSecretAsync(Id),
        c => c.WebhookEndpoints.Deliveries.ListAsync(Id),
        c => c.WebhookEndpoints.Deliveries.RetrieveAsync(Id, Delivery),
        c => c.WebhookEndpoints.Deliveries.ReplayAsync(Id, Delivery),
        // events
        c => c.Events.ListAsync(),
        c => c.Events.RetrieveAsync(Id),
        c => c.Events.CatalogAsync(),
        // organization
        c => c.Organization.RetrieveAsync(),
        c => c.Organization.UpdateAsync(new TenantSettingsUpdateParams { SettlementMode = "split_at_collection" }),
        c => c.Organization.Billing.RetrieveAsync(),
        c => c.Organization.Billing.UpdateAsync(new BillingSettingsUpdateParams { CommsEnabled = true }),
        // metrics
        c => c.Metrics.BillingAsync(),
        // sandbox
        c => c.Sandbox.CreatePaymentMethodAsync(new SandboxPaymentMethodParams { CustomerId = Id }),
        c => c.Sandbox.AdvanceCycleAsync(Id),
        c => c.Sandbox.SimulateWebhookAsync(new SandboxSimulateWebhookParams { Type = "invoice.paid" }),
    };

    [Fact]
    public async Task Every_sdk_call_matches_a_spec_operation_and_every_spec_operation_is_covered()
    {
        var specOps = LoadSpecOps();

        const string universal = "{\"success\":true,\"statusCode\":200,\"data\":{}," +
            "\"pagination\":{\"limit\":20,\"hasMore\":false,\"nextCursor\":null},\"meta\":{\"requestId\":\"req_conf\"}}";
        var handler = new MockHttpHandler((_, _, _) => Task.FromResult(Responses.Json(HttpStatusCode.OK, universal)));
        var client = new Nombaone(new NombaoneOptions
        {
            ApiKey = "nbo_sandbox_conformance",
            BaseUrl = "http://api.test",
            MaxRetries = 0,
            HttpClient = new HttpClient(handler),
        });

        // We only care that each method emits its route; the canned {} body will
        // fail to deserialize for list methods (array expected) — that's fine.
        foreach (var exercise in Exercises)
        {
            try
            {
                await exercise(client);
            }
            catch
            {
                // ignored — the request is already recorded before the body is parsed
            }
        }

        Assert.Equal(Exercises.Length, handler.Requests.Count); // every method fired exactly one request

        var covered = new HashSet<string>();
        var unmatched = new List<string>();
        foreach (var request in handler.Requests)
        {
            var method = request.Method.Method.ToLowerInvariant();
            var match = MatchSpecOp(specOps, method, request.Path);
            if (match is null)
            {
                unmatched.Add($"{method} {request.Path}");
            }
            else
            {
                covered.Add(match.Key);
            }
        }

        Assert.True(unmatched.Count == 0, "SDK emitted routes not in the spec: " + string.Join(", ", unmatched));

        var missing = specOps
            .Select(op => op.Key)
            .Where(key => !Excluded.Contains(key) && !covered.Contains(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();
        Assert.True(missing.Count == 0, "spec operations with no SDK method: " + string.Join(", ", missing));

        // Belt-and-braces: every exclusion still exists in the spec, so a renamed
        // route can't silently hide behind the exclusion list.
        foreach (var excluded in Excluded)
        {
            Assert.True(specOps.Any(op => op.Key == excluded), $"EXCLUDED entry no longer in spec: {excluded}");
        }
    }

    private static List<SpecOp> LoadSpecOps()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "spec", "openapi.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var paths = document.RootElement.GetProperty("paths");

        var ops = new List<SpecOp>();
        foreach (var pathEntry in paths.EnumerateObject())
        {
            var route = pathEntry.Name;
            var segments = route.Split('/').Where(s => s.Length > 0).ToArray();
            foreach (var methodEntry in pathEntry.Value.EnumerateObject())
            {
                var method = methodEntry.Name.ToLowerInvariant();
                if (HttpMethods.Contains(method))
                {
                    ops.Add(new SpecOp(method, segments, $"{method} {route}"));
                }
            }
        }

        return ops;
    }

    // Most-specific structural match: {param} matches any segment; literals win ties.
    private static SpecOp? MatchSpecOp(List<SpecOp> specOps, string method, string urlPath)
    {
        var segments = urlPath.Split('/').Where(s => s.Length > 0).ToArray();
        SpecOp? best = null;
        var bestLiterals = -1;

        foreach (var op in specOps)
        {
            if (op.Method != method || op.Segments.Length != segments.Length)
            {
                continue;
            }

            var literals = 0;
            var ok = true;
            for (var i = 0; i < segments.Length; i++)
            {
                var specSegment = op.Segments[i];
                if (specSegment.StartsWith("{", StringComparison.Ordinal))
                {
                    continue;
                }

                if (specSegment != segments[i])
                {
                    ok = false;
                    break;
                }

                literals++;
            }

            if (ok && literals > bestLiterals)
            {
                best = op;
                bestLiterals = literals;
            }
        }

        return best;
    }
}
