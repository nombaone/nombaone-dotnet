using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NombaOne;
using Xunit;
using Xunit.Abstractions;

namespace NombaOne.Tests.Integration;

/// <summary>
/// Exhaustive live coverage: every one of the SDK's ~78 method call-sites is
/// exercised against a real NombaOne API. Each call must either succeed or fail
/// with a <b>specific expected</b> typed error — any other outcome (a wrong path,
/// verb, body, or an unexpected error) fails the run and names the method.
/// Gated: set NOMBAONE_INTEGRATION=1 and NOMBAONE_API_KEY to run.
/// </summary>
[Trait("Category", "Integration")]
public class FullSurfaceLiveTests
{
    private readonly ITestOutputHelper _out;

    public FullSurfaceLiveTests(ITestOutputHelper output) => _out = output;

    private static bool Enabled => Environment.GetEnvironmentVariable("NOMBAONE_INTEGRATION") == "1";

    private static Nombaone NewClient() => new(new NombaoneOptions
    {
        ApiKey = Environment.GetEnvironmentVariable("NOMBAONE_API_KEY"),
        BaseUrl = Environment.GetEnvironmentVariable("NOMBAONE_BASE_URL"),
    });

    private static string Tag() => Guid.NewGuid().ToString("N").Substring(0, 10);

    private async Task<T> Must<T>(string label, Func<Task<T>> call)
    {
        try
        {
            var result = await call();
            _out.WriteLine($"OK    {label}");
            return result;
        }
        catch (Exception ex)
        {
            _out.WriteLine($"FAIL  {label} -> {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }

    private async Task<T?> TryOk<T>(string label, Func<Task<T>> call, params string[] acceptableCodes)
        where T : class
    {
        try
        {
            var result = await call();
            _out.WriteLine($"OK    {label}");
            return result;
        }
        catch (NombaoneApiException ex) when (acceptableCodes.Contains(ex.Code))
        {
            _out.WriteLine($"OK    {label} -> expected {ex.Code}");
            return null;
        }
        catch (Exception ex)
        {
            _out.WriteLine($"FAIL  {label} -> {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }

    [SkippableFact]
    public async Task Every_method_works_end_to_end_against_the_sandbox()
    {
        Skip.IfNot(Enabled, "Set NOMBAONE_INTEGRATION=1 and NOMBAONE_API_KEY to run.");
        using var client = NewClient();
        var tag = Tag();

        // ---------- customers ----------
        var customer = await Must("customers.create", () => client.Customers.CreateAsync(new CustomerCreateParams { Email = $"full-{tag}@example.com", Name = "Full Surface" }));
        await Must("customers.retrieve", () => client.Customers.RetrieveAsync(customer.Id));
        await Must("customers.update", () => client.Customers.UpdateAsync(customer.Id, new CustomerUpdateParams { Name = "Full Surface II", Phone = "+2348012345678" }));
        await Must("customers.update(clear phone)", () => client.Customers.UpdateAsync(customer.Id, new CustomerUpdateParams { Phone = Optional<string>.Null }));
        await Must("customers.list", () => client.Customers.ListAsync(new CustomerListParams { Limit = 3 }));
        var grant = await Must("customers.grantCredit", () => client.Customers.GrantCreditAsync(customer.Id, new CustomerGrantCreditParams { AmountInKobo = 100_000, Source = "goodwill" }));
        await Must("customers.retrieveCreditBalance", () => client.Customers.RetrieveCreditBalanceAsync(customer.Id));
        await Must("customers.voidCredit", () => client.Customers.VoidCreditAsync(customer.Id, grant.Id));

        // ---------- coupons ----------
        var couponA = await Must("coupons.create(A)", () => client.Coupons.CreateAsync(new CouponCreateParams { Code = $"CUST{tag}", PercentOff = 10, Duration = "forever" }));
        var couponB = await Must("coupons.create(B)", () => client.Coupons.CreateAsync(new CouponCreateParams { Code = $"SUB{tag}", PercentOff = 15, Duration = "forever" }));
        await Must("coupons.retrieve", () => client.Coupons.RetrieveAsync(couponA.Id));
        await Must("coupons.update", () => client.Coupons.UpdateAsync(couponA.Id, new CouponUpdateParams { MaxRedemptions = 1000 }));
        await Must("coupons.list", () => client.Coupons.ListAsync(new CouponListParams { Limit = 3 }));

        // customer discount (needs a coupon)
        await Must("customers.applyDiscount", () => client.Customers.ApplyDiscountAsync(customer.Id, new CustomerApplyDiscountParams { Coupon = couponA.Id }));
        await Must("customers.removeDiscount", () => client.Customers.RemoveDiscountAsync(customer.Id));

        // ---------- plans (+ prices) ----------
        var plan = await Must("plans.create", () => client.Plans.CreateAsync(new PlanCreateParams { Name = $"Full {tag}", Description = "surface plan" }));
        await Must("plans.retrieve", () => client.Plans.RetrieveAsync(plan.Id));
        await Must("plans.update", () => client.Plans.UpdateAsync(plan.Id, new PlanUpdateParams { Description = "updated" }));
        await Must("plans.update(clear description)", () => client.Plans.UpdateAsync(plan.Id, new PlanUpdateParams { Description = Optional<string>.Null }));
        await Must("plans.list", () => client.Plans.ListAsync(new PlanListParams { Limit = 3 }));
        var price1 = await Must("plans.prices.create(1)", () => client.Plans.Prices.CreateAsync(plan.Id, new PriceCreateParams { UnitAmountInKobo = 250_000, Interval = "month" }));
        var price2 = await Must("plans.prices.create(2)", () => client.Plans.Prices.CreateAsync(plan.Id, new PriceCreateParams { UnitAmountInKobo = 500_000, Interval = "month" }));
        await Must("plans.prices.list", () => client.Plans.Prices.ListAsync(plan.Id));

        // throwaway plan+price for archive/deactivate happy paths
        var planB = await Must("plans.create(throwaway)", () => client.Plans.CreateAsync(new PlanCreateParams { Name = $"Throw {tag}" }));
        var priceC = await Must("plans.prices.create(throwaway)", () => client.Plans.Prices.CreateAsync(planB.Id, new PriceCreateParams { UnitAmountInKobo = 100_000, Interval = "month" }));
        await Must("prices.deactivate", () => client.Prices.DeactivateAsync(priceC.Id));
        await Must("plans.archive", () => client.Plans.ArchiveAsync(planB.Id));

        // ---------- prices ----------
        await Must("prices.retrieve", () => client.Prices.RetrieveAsync(price1.Id));
        await Must("prices.list", () => client.Prices.ListAsync(new PriceListParams { PlanRef = plan.Id, Active = true }));

        // ---------- payment methods (+ sandbox) ----------
        var method1 = await Must("sandbox.createPaymentMethod(1)", () => client.Sandbox.CreatePaymentMethodAsync(new SandboxPaymentMethodParams { CustomerId = customer.Id }));
        var method2 = await Must("sandbox.createPaymentMethod(2)", () => client.Sandbox.CreatePaymentMethodAsync(new SandboxPaymentMethodParams { CustomerId = customer.Id }));
        var method3 = await Must("sandbox.createPaymentMethod(3)", () => client.Sandbox.CreatePaymentMethodAsync(new SandboxPaymentMethodParams { CustomerId = customer.Id }));
        await Must("paymentMethods.setup", () => client.PaymentMethods.SetupAsync(new PaymentMethodSetupParams { CustomerRef = customer.Id, AmountInKobo = 5_000, CallbackUrl = "https://example.com/return" }));
        await TryOk("paymentMethods.createVirtualAccount", () => client.PaymentMethods.CreateVirtualAccountAsync(new PaymentMethodVirtualAccountParams { CustomerRef = customer.Id }), NombaoneErrorCodes.SystemUpstreamError, NombaoneErrorCodes.PaymentMethodKindMismatch);
        await Must("paymentMethods.retrieve", () => client.PaymentMethods.RetrieveAsync(method1.Id));
        await Must("paymentMethods.list", () => client.PaymentMethods.ListAsync(new PaymentMethodListParams { CustomerRef = customer.Id }));
        await Must("paymentMethods.setDefault", () => client.PaymentMethods.SetDefaultAsync(method1.Id));
        await Must("paymentMethods.remove", () => client.PaymentMethods.RemoveAsync(method3.Id));

        // ---------- mandates (NIBSS may 504 on sandbox) ----------
        await TryOk("mandates.create", () => client.Mandates.CreateAsync(new MandateCreateParams
        {
            CustomerRef = customer.Id,
            CustomerAccountNumber = "0123456789",
            BankCode = "058",
            CustomerName = "Full Surface",
            CustomerAccountName = "Full Surface",
            CustomerPhoneNumber = "+2348012345678",
            CustomerAddress = "1 Marina, Lagos",
            Narration = "full surface mandate",
            MaxAmountInKobo = 500_000,
        }, new RequestOptions { MaxRetries = 0 }), NombaoneErrorCodes.SystemUpstreamError, NombaoneErrorCodes.SystemInternalError);
        await TryOk("mandates.retrieve", () => client.Mandates.RetrieveAsync(method1.Id), NombaoneErrorCodes.PaymentMethodKindMismatch, NombaoneErrorCodes.PaymentMethodNotFound);

        // ---------- subscriptions (+ schedule + dunning) ----------
        var sub = await Must("subscriptions.create", () => client.Subscriptions.CreateAsync(new SubscriptionCreateParams { CustomerId = customer.Id, PriceId = price1.Id, PaymentMethodId = method1.Id }));
        await Must("subscriptions.retrieve", () => client.Subscriptions.RetrieveAsync(sub.Id));
        await Must("subscriptions.update", () => client.Subscriptions.UpdateAsync(sub.Id, new SubscriptionUpdateParams { Metadata = new Dictionary<string, object?> { ["tier"] = "gold" } }));
        await Must("subscriptions.list", () => client.Subscriptions.ListAsync(new SubscriptionListParams { CustomerId = customer.Id }));
        await Must("subscriptions.listEvents", () => client.Subscriptions.ListEventsAsync(sub.Id));
        await Must("subscriptions.retrieveUpcomingInvoice", () => client.Subscriptions.RetrieveUpcomingInvoiceAsync(sub.Id));
        await Must("subscriptions.dunning.retrieve", () => client.Subscriptions.Dunning.RetrieveAsync(sub.Id));
        await Must("subscriptions.dunning.listAttempts", () => client.Subscriptions.Dunning.ListAttemptsAsync(sub.Id));
        await Must("subscriptions.applyDiscount", () => client.Subscriptions.ApplyDiscountAsync(sub.Id, new SubscriptionApplyDiscountParams { Coupon = couponB.Id }));
        await Must("subscriptions.removeDiscount", () => client.Subscriptions.RemoveDiscountAsync(sub.Id));
        await Must("subscriptions.schedule.create", () => client.Subscriptions.Schedule.CreateAsync(sub.Id, new SubscriptionScheduleCreateParams { PriceId = price2.Id }));
        await Must("subscriptions.schedule.retrieve", () => client.Subscriptions.Schedule.RetrieveAsync(sub.Id));
        await Must("subscriptions.schedule.release", () => client.Subscriptions.Schedule.ReleaseAsync(sub.Id));
        await Must("subscriptions.change", () => client.Subscriptions.ChangeAsync(sub.Id, new SubscriptionChangeParams { PriceId = price2.Id }));
        await Must("subscriptions.updatePaymentMethod", () => client.Subscriptions.UpdatePaymentMethodAsync(sub.Id, new SubscriptionUpdatePaymentMethodParams { PaymentMethodReference = method2.Id }));
        await Must("subscriptions.pause", () => client.Subscriptions.PauseAsync(sub.Id));
        await Must("subscriptions.resume", () => client.Subscriptions.ResumeAsync(sub.Id));

        // sandbox advance-cycle produces a real invoice on this subscription
        var cycle = await Must("sandbox.advanceCycle", () => client.Sandbox.AdvanceCycleAsync(sub.Id));

        await Must("subscriptions.cancel", () => client.Subscriptions.CancelAsync(sub.Id, new SubscriptionCancelParams { Mode = "now" }));
        await Must("subscriptions.resubscribe", () => client.Subscriptions.ResubscribeAsync(sub.Id));

        // ---------- invoices ----------
        await Must("invoices.retrieve", () => client.Invoices.RetrieveAsync(cycle.Invoice.Id));
        await Must("invoices.list", () => client.Invoices.ListAsync(new InvoiceListParams { CustomerId = customer.Id }));
        // A voidable invoice: a send_invoice subscription, advanced to issue an open (uncharged) invoice.
        var subInvoice = await Must("subscriptions.create(send_invoice)", () => client.Subscriptions.CreateAsync(new SubscriptionCreateParams { CustomerId = customer.Id, PriceId = price1.Id, CollectionMethod = "send_invoice" }));
        var openCycle = await Must("sandbox.advanceCycle(send_invoice)", () => client.Sandbox.AdvanceCycleAsync(subInvoice.Id));
        _out.WriteLine($"      send_invoice cycle invoice {openCycle.Invoice.Id} status={openCycle.Invoice.Status}");
        await TryOk("invoices.void", () => client.Invoices.VoidAsync(openCycle.Invoice.Id, new InvoiceVoidParams { Comment = "full surface void" }), NombaoneErrorCodes.InvoiceNotVoidable, NombaoneErrorCodes.InvoiceAlreadyPaid);

        // ---------- settlements ----------
        await TryOk("settlements.retrieve", () => client.Settlements.RetrieveAsync("nbo000000000000stl"), NombaoneErrorCodes.SettlementNotFound, NombaoneErrorCodes.SettlementSubaccountNotFound);
        await Must("settlements.list", () => client.Settlements.ListAsync(new SettlementListParams { Limit = 3 }));
        await TryOk("settlements.retrieveEscrow", () => client.Settlements.RetrieveEscrowAsync(), NombaoneErrorCodes.SettlementSubaccountNotFound);
        await TryOk("settlements.refund", () => client.Settlements.RefundAsync("nbo000000000000stl"), NombaoneErrorCodes.SettlementNotFound, NombaoneErrorCodes.SettlementSubaccountNotFound);
        await TryOk("settlements.createPayout", () => client.Settlements.CreatePayoutAsync(new PayoutCreateParams { AmountInKobo = 100_000, BankCode = "058", AccountNumber = "0123456789" }, new RequestOptions { IdempotencyKey = $"payout-{tag}" }), NombaoneErrorCodes.EscrowLocked, NombaoneErrorCodes.PayoutExceedsAvailable, NombaoneErrorCodes.SettlementSubaccountNotFound);

        // ---------- webhook endpoints (+ deliveries) ----------
        var endpoint = await Must("webhookEndpoints.create", () => client.WebhookEndpoints.CreateAsync(new WebhookEndpointCreateParams { Url = $"https://example.com/hooks/{tag}", EnabledEvents = new[] { "*" } }));
        Assert.False(string.IsNullOrEmpty(endpoint.SigningSecret)); // shown once
        await Must("webhookEndpoints.retrieve", () => client.WebhookEndpoints.RetrieveAsync(endpoint.Id));
        await Must("webhookEndpoints.update", () => client.WebhookEndpoints.UpdateAsync(endpoint.Id, new WebhookEndpointUpdateParams { Disabled = false }));
        await Must("webhookEndpoints.list", () => client.WebhookEndpoints.ListAsync());
        await Must("webhookEndpoints.rotateSecret", () => client.WebhookEndpoints.RotateSecretAsync(endpoint.Id));
        await Must("sandbox.simulateWebhook", () => client.Sandbox.SimulateWebhookAsync(new SandboxSimulateWebhookParams { Type = "invoice.paid" }));
        var deliveries = await Must("webhookEndpoints.deliveries.list", () => client.WebhookEndpoints.Deliveries.ListAsync(endpoint.Id));
        var deliveryId = deliveries.Data.FirstOrDefault()?.Id;
        if (deliveryId is not null)
        {
            await Must("webhookEndpoints.deliveries.retrieve", () => client.WebhookEndpoints.Deliveries.RetrieveAsync(endpoint.Id, deliveryId));
            await TryOk("webhookEndpoints.deliveries.replay", () => client.WebhookEndpoints.Deliveries.ReplayAsync(endpoint.Id, deliveryId), NombaoneErrorCodes.ClientConflict);
        }
        else
        {
            await TryOk("webhookEndpoints.deliveries.retrieve(none yet)", () => client.WebhookEndpoints.Deliveries.RetrieveAsync(endpoint.Id, "nbo000000000000whd"), NombaoneErrorCodes.ClientResourceNotFound);
            await TryOk("webhookEndpoints.deliveries.replay(none yet)", () => client.WebhookEndpoints.Deliveries.ReplayAsync(endpoint.Id, "nbo000000000000whd"), NombaoneErrorCodes.ClientResourceNotFound);
        }

        var endpointB = await Must("webhookEndpoints.create(throwaway)", () => client.WebhookEndpoints.CreateAsync(new WebhookEndpointCreateParams { Url = $"https://example.com/hooks/del-{tag}" }));
        await Must("webhookEndpoints.delete", () => client.WebhookEndpoints.DeleteAsync(endpointB.Id));

        // ---------- events ----------
        var events = await Must("events.list", () => client.Events.ListAsync(new EventListParams { Limit = 3 }));
        var eventId = events.Data.FirstOrDefault()?.Id;
        if (eventId is not null)
        {
            await Must("events.retrieve", () => client.Events.RetrieveAsync(eventId));
        }
        else
        {
            await TryOk("events.retrieve(none)", () => client.Events.RetrieveAsync("nbo000000000000evt"), NombaoneErrorCodes.ClientResourceNotFound);
        }

        await Must("events.catalog", async () => (object)await client.Events.CatalogAsync());

        // ---------- organization (+ billing) ----------
        await Must("organization.retrieve", () => client.Organization.RetrieveAsync());
        await Must("organization.update", () => client.Organization.UpdateAsync(new TenantSettingsUpdateParams { Branding = new BrandingParams { DisplayName = "Full Surface Co" } }));
        await Must("organization.billing.retrieve", () => client.Organization.Billing.RetrieveAsync());
        await Must("organization.billing.update", () => client.Organization.Billing.UpdateAsync(new BillingSettingsUpdateParams { CommsEnabled = true }));

        // ---------- metrics ----------
        await Must("metrics.billing", () => client.Metrics.BillingAsync());

        _out.WriteLine("ALL METHODS EXERCISED.");
    }
}
