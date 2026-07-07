using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NombaOne;

namespace NombaOne.Examples;

/// <summary>
/// Owner-readable full-surface live verification. Calls every SDK method against
/// the real sandbox and checks that each returned object's wire <c>domain</c>
/// matches what the model claims — a silently mis-parsed response is a DEFECT, a
/// typed API error is expected. Prints a per-method line and a one-line verdict;
/// exits non-zero if any defect.
/// </summary>
internal static class Verify
{
    private sealed class Report
    {
        public int Total;
        public int Ok;
        public int ExpectedErrors;
        public int Defects;
        public readonly List<string> Lines = new();
    }

    internal static async Task RunAsync()
    {
        using var client = new Nombaone();
        var r = new Report();
        var tag = Guid.NewGuid().ToString("N").Substring(0, 10);

        Console.WriteLine($"NombaOne .NET SDK — full-surface live verification against {client.BaseUrl} (mode: {client.Mode})");
        Console.WriteLine(new string('-', 78));

        async Task<T?> Check<T>(string name, string? expectedDomain, Func<Task<T>> call, params string[] okCodes)
            where T : class
        {
            r.Total++;
            try
            {
                var result = await call();
                if (expectedDomain is not null)
                {
                    var domain = result.GetType().GetProperty("Domain")?.GetValue(result) as string;
                    if (domain is not null && domain != expectedDomain)
                    {
                        r.Defects++;
                        r.Lines.Add($"DEFECT   {name}: model claims domain '{expectedDomain}', wire returned '{domain}'");
                        return result;
                    }
                }

                r.Ok++;
                r.Lines.Add($"ok       {name}");
                return result;
            }
            catch (NombaoneApiException ex) when (okCodes.Contains(ex.Code))
            {
                r.ExpectedErrors++;
                r.Lines.Add($"ok(err)  {name} -> {ex.Code}");
                return null;
            }
            catch (Exception ex)
            {
                r.Defects++;
                r.Lines.Add($"DEFECT   {name} -> {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        // ---- fixtures ----
        var customer = (await Check("customers.create", "customer", () => client.Customers.CreateAsync(new CustomerCreateParams { Email = $"verify-{tag}@example.com", Name = "Verify" })))!;
        var couponA = (await Check("coupons.create(A)", "coupon", () => client.Coupons.CreateAsync(new CouponCreateParams { Code = $"VA{tag}", PercentOff = 10, Duration = "forever" })))!;
        var couponB = (await Check("coupons.create(B)", "coupon", () => client.Coupons.CreateAsync(new CouponCreateParams { Code = $"VB{tag}", PercentOff = 15, Duration = "forever" })))!;
        var plan = (await Check("plans.create", "plan", () => client.Plans.CreateAsync(new PlanCreateParams { Name = $"Verify {tag}" })))!;
        var price1 = (await Check("plans.prices.create(1)", "price", () => client.Plans.Prices.CreateAsync(plan.Id, new PriceCreateParams { UnitAmountInKobo = 250_000, Interval = "month" })))!;
        var price2 = (await Check("plans.prices.create(2)", "price", () => client.Plans.Prices.CreateAsync(plan.Id, new PriceCreateParams { UnitAmountInKobo = 500_000, Interval = "month" })))!;
        var method1 = (await Check("sandbox.createPaymentMethod(1)", "payment_method", () => client.Sandbox.CreatePaymentMethodAsync(new SandboxPaymentMethodParams { CustomerId = customer.Id })))!;
        var method2 = (await Check("sandbox.createPaymentMethod(2)", "payment_method", () => client.Sandbox.CreatePaymentMethodAsync(new SandboxPaymentMethodParams { CustomerId = customer.Id })))!;
        var method3 = (await Check("sandbox.createPaymentMethod(3)", "payment_method", () => client.Sandbox.CreatePaymentMethodAsync(new SandboxPaymentMethodParams { CustomerId = customer.Id })))!;
        var sub = (await Check("subscriptions.create", "subscription", () => client.Subscriptions.CreateAsync(new SubscriptionCreateParams { CustomerId = customer.Id, PriceId = price1.Id, PaymentMethodId = method1.Id })))!;

        // ---- customers ----
        await Check("customers.retrieve", "customer", () => client.Customers.RetrieveAsync(customer.Id));
        await Check("customers.update", "customer", () => client.Customers.UpdateAsync(customer.Id, new CustomerUpdateParams { Name = "Verify II" }));
        await Check("customers.list", null, () => client.Customers.ListAsync(new CustomerListParams { Limit = 3 }));
        var grant = await Check("customers.grantCredit", "credit_grant", () => client.Customers.GrantCreditAsync(customer.Id, new CustomerGrantCreditParams { AmountInKobo = 100_000, Source = "goodwill" }));
        await Check("customers.retrieveCreditBalance", "credit_balance", () => client.Customers.RetrieveCreditBalanceAsync(customer.Id));
        if (grant is not null)
        {
            await Check("customers.voidCredit", "credit_grant", () => client.Customers.VoidCreditAsync(customer.Id, grant.Id));
        }

        await Check("customers.applyDiscount", "discount", () => client.Customers.ApplyDiscountAsync(customer.Id, new CustomerApplyDiscountParams { Coupon = couponA.Id }));
        await Check("customers.removeDiscount", "discount", () => client.Customers.RemoveDiscountAsync(customer.Id));

        // ---- coupons ----
        await Check("coupons.retrieve", "coupon", () => client.Coupons.RetrieveAsync(couponA.Id));
        await Check("coupons.update", "coupon", () => client.Coupons.UpdateAsync(couponA.Id, new CouponUpdateParams { MaxRedemptions = 1000 }));
        await Check("coupons.list", null, () => client.Coupons.ListAsync());

        // ---- plans / prices ----
        await Check("plans.retrieve", "plan", () => client.Plans.RetrieveAsync(plan.Id));
        await Check("plans.update", "plan", () => client.Plans.UpdateAsync(plan.Id, new PlanUpdateParams { Description = "verified" }));
        await Check("plans.list", null, () => client.Plans.ListAsync());
        await Check("plans.prices.list", null, () => client.Plans.Prices.ListAsync(plan.Id));
        var planB = await Check("plans.create(throwaway)", "plan", () => client.Plans.CreateAsync(new PlanCreateParams { Name = $"Throw {tag}" }));
        if (planB is not null)
        {
            var priceC = await Check("plans.prices.create(throwaway)", "price", () => client.Plans.Prices.CreateAsync(planB.Id, new PriceCreateParams { UnitAmountInKobo = 100_000, Interval = "month" }));
            if (priceC is not null)
            {
                await Check("prices.deactivate", "price", () => client.Prices.DeactivateAsync(priceC.Id));
            }

            await Check("plans.archive", "plan", () => client.Plans.ArchiveAsync(planB.Id));
        }

        await Check("prices.retrieve", "price", () => client.Prices.RetrieveAsync(price1.Id));
        await Check("prices.list", null, () => client.Prices.ListAsync(new PriceListParams { PlanRef = plan.Id }));

        // ---- payment methods ----
        await Check("paymentMethods.setup", "checkout_setup", () => client.PaymentMethods.SetupAsync(new PaymentMethodSetupParams { CustomerRef = customer.Id, AmountInKobo = 5_000, CallbackUrl = "https://example.com/return" }));
        await Check("paymentMethods.createVirtualAccount", "virtual_account", () => client.PaymentMethods.CreateVirtualAccountAsync(new PaymentMethodVirtualAccountParams { CustomerRef = customer.Id }), NombaoneErrorCodes.SystemUpstreamError);
        await Check("paymentMethods.retrieve", "payment_method", () => client.PaymentMethods.RetrieveAsync(method1.Id));
        await Check("paymentMethods.list", null, () => client.PaymentMethods.ListAsync(new PaymentMethodListParams { CustomerRef = customer.Id }));
        await Check("paymentMethods.setDefault", "payment_method", () => client.PaymentMethods.SetDefaultAsync(method1.Id));
        await Check("paymentMethods.remove", "payment_method", () => client.PaymentMethods.RemoveAsync(method3.Id));

        // ---- mandates ----
        await Check("mandates.create", "mandate_setup", () => client.Mandates.CreateAsync(new MandateCreateParams
        {
            CustomerRef = customer.Id,
            CustomerAccountNumber = "0123456789",
            BankCode = "058",
            CustomerName = "Verify",
            CustomerAccountName = "Verify",
            CustomerPhoneNumber = "+2348012345678",
            CustomerAddress = "Lagos",
            Narration = "verify mandate",
            MaxAmountInKobo = 500_000,
        }, new RequestOptions { MaxRetries = 0 }), NombaoneErrorCodes.SystemUpstreamError, NombaoneErrorCodes.SystemInternalError);
        await Check("mandates.retrieve", "payment_method", () => client.Mandates.RetrieveAsync(method1.Id), NombaoneErrorCodes.PaymentMethodKindMismatch, NombaoneErrorCodes.PaymentMethodNotFound);

        // ---- subscriptions (+ schedule + dunning) ----
        await Check("subscriptions.retrieve", "subscription", () => client.Subscriptions.RetrieveAsync(sub.Id));
        await Check("subscriptions.update", "subscription", () => client.Subscriptions.UpdateAsync(sub.Id, new SubscriptionUpdateParams { Metadata = new Dictionary<string, object?> { ["k"] = "v" } }));
        await Check("subscriptions.list", null, () => client.Subscriptions.ListAsync(new SubscriptionListParams { CustomerId = customer.Id }));
        await Check("subscriptions.listEvents", null, () => client.Subscriptions.ListEventsAsync(sub.Id));
        await Check("subscriptions.retrieveUpcomingInvoice", "upcoming_invoice", () => client.Subscriptions.RetrieveUpcomingInvoiceAsync(sub.Id));
        await Check("subscriptions.dunning.retrieve", "dunning_state", () => client.Subscriptions.Dunning.RetrieveAsync(sub.Id));
        await Check("subscriptions.dunning.listAttempts", null, () => client.Subscriptions.Dunning.ListAttemptsAsync(sub.Id));
        await Check("subscriptions.applyDiscount", "discount", () => client.Subscriptions.ApplyDiscountAsync(sub.Id, new SubscriptionApplyDiscountParams { Coupon = couponB.Id }));
        await Check("subscriptions.removeDiscount", "discount", () => client.Subscriptions.RemoveDiscountAsync(sub.Id));
        await Check("subscriptions.schedule.create", "subscription_schedule", () => client.Subscriptions.Schedule.CreateAsync(sub.Id, new SubscriptionScheduleCreateParams { PriceId = price2.Id }));
        await Check("subscriptions.schedule.retrieve", "subscription_schedule", () => client.Subscriptions.Schedule.RetrieveAsync(sub.Id));
        await Check("subscriptions.schedule.release", "subscription_schedule", () => client.Subscriptions.Schedule.ReleaseAsync(sub.Id));
        await Check("subscriptions.change", "subscription", () => client.Subscriptions.ChangeAsync(sub.Id, new SubscriptionChangeParams { PriceId = price2.Id }));
        await Check("subscriptions.updatePaymentMethod", "payment_method", () => client.Subscriptions.UpdatePaymentMethodAsync(sub.Id, new SubscriptionUpdatePaymentMethodParams { PaymentMethodReference = method2.Id }));
        await Check("subscriptions.pause", "subscription", () => client.Subscriptions.PauseAsync(sub.Id));
        await Check("subscriptions.resume", "subscription", () => client.Subscriptions.ResumeAsync(sub.Id));
        var cycle = await Check("sandbox.advanceCycle", "advance_cycle_result", () => client.Sandbox.AdvanceCycleAsync(sub.Id));
        await Check("subscriptions.cancel", "subscription", () => client.Subscriptions.CancelAsync(sub.Id));
        await Check("subscriptions.resubscribe", "subscription", () => client.Subscriptions.ResubscribeAsync(sub.Id));

        // ---- invoices ----
        if (cycle is not null)
        {
            await Check("invoices.retrieve", "invoice", () => client.Invoices.RetrieveAsync(cycle.Invoice.Id));
        }

        await Check("invoices.list", null, () => client.Invoices.ListAsync(new InvoiceListParams { CustomerId = customer.Id }));
        var subInvoice = await Check("subscriptions.create(send_invoice)", "subscription", () => client.Subscriptions.CreateAsync(new SubscriptionCreateParams { CustomerId = customer.Id, PriceId = price1.Id, CollectionMethod = "send_invoice" }));
        if (subInvoice is not null)
        {
            var openCycle = await Check("sandbox.advanceCycle(send_invoice)", "advance_cycle_result", () => client.Sandbox.AdvanceCycleAsync(subInvoice.Id));
            if (openCycle is not null)
            {
                await Check("invoices.void", "invoice", () => client.Invoices.VoidAsync(openCycle.Invoice.Id), NombaoneErrorCodes.InvoiceNotVoidable, NombaoneErrorCodes.InvoiceAlreadyPaid);
            }
        }

        // ---- settlements (sandbox subaccount usually not configured) ----
        await Check("settlements.retrieve", "settlement", () => client.Settlements.RetrieveAsync("nbo000000000000stl"), NombaoneErrorCodes.SettlementNotFound, NombaoneErrorCodes.SettlementSubaccountNotFound);
        await Check("settlements.list", null, () => client.Settlements.ListAsync());
        await Check("settlements.retrieveEscrow", "escrow", () => client.Settlements.RetrieveEscrowAsync(), NombaoneErrorCodes.SettlementSubaccountNotFound);
        await Check("settlements.refund", "refund", () => client.Settlements.RefundAsync("nbo000000000000stl"), NombaoneErrorCodes.SettlementNotFound, NombaoneErrorCodes.SettlementSubaccountNotFound);
        await Check("settlements.createPayout", "payout", () => client.Settlements.CreatePayoutAsync(new PayoutCreateParams { AmountInKobo = 100_000, BankCode = "058", AccountNumber = "0123456789" }, new RequestOptions { IdempotencyKey = $"vp-{tag}" }), NombaoneErrorCodes.EscrowLocked, NombaoneErrorCodes.PayoutExceedsAvailable, NombaoneErrorCodes.SettlementSubaccountNotFound);

        // ---- webhook endpoints (+ deliveries) ----
        var endpoint = await Check("webhookEndpoints.create", "webhook", () => client.WebhookEndpoints.CreateAsync(new WebhookEndpointCreateParams { Url = $"https://example.com/hooks/{tag}" }));
        if (endpoint is not null)
        {
            await Check("webhookEndpoints.retrieve", "webhook", () => client.WebhookEndpoints.RetrieveAsync(endpoint.Id));
            await Check("webhookEndpoints.update", "webhook", () => client.WebhookEndpoints.UpdateAsync(endpoint.Id, new WebhookEndpointUpdateParams { Disabled = false }));
            await Check("webhookEndpoints.list", null, () => client.WebhookEndpoints.ListAsync());
            await Check("webhookEndpoints.rotateSecret", "webhook_secret", () => client.WebhookEndpoints.RotateSecretAsync(endpoint.Id));
            await Check("sandbox.simulateWebhook", "webhook_simulation", () => client.Sandbox.SimulateWebhookAsync(new SandboxSimulateWebhookParams { Type = "invoice.paid" }));
            var deliveries = await Check("webhookEndpoints.deliveries.list", null, () => client.WebhookEndpoints.Deliveries.ListAsync(endpoint.Id));
            var deliveryId = deliveries?.Data.FirstOrDefault()?.Id;
            if (deliveryId is not null)
            {
                await Check("webhookEndpoints.deliveries.retrieve", "webhook_delivery", () => client.WebhookEndpoints.Deliveries.RetrieveAsync(endpoint.Id, deliveryId));
                await Check("webhookEndpoints.deliveries.replay", "webhook_delivery", () => client.WebhookEndpoints.Deliveries.ReplayAsync(endpoint.Id, deliveryId), NombaoneErrorCodes.ClientConflict);
            }
            else
            {
                await Check("webhookEndpoints.deliveries.retrieve", "webhook_delivery", () => client.WebhookEndpoints.Deliveries.RetrieveAsync(endpoint.Id, "nbo000000000000whd"), NombaoneErrorCodes.ClientResourceNotFound);
                await Check("webhookEndpoints.deliveries.replay", "webhook_delivery", () => client.WebhookEndpoints.Deliveries.ReplayAsync(endpoint.Id, "nbo000000000000whd"), NombaoneErrorCodes.ClientResourceNotFound);
            }

            var endpointB = await Check("webhookEndpoints.create(throwaway)", "webhook", () => client.WebhookEndpoints.CreateAsync(new WebhookEndpointCreateParams { Url = $"https://example.com/hooks/del-{tag}" }));
            if (endpointB is not null)
            {
                await Check("webhookEndpoints.delete", "webhook", () => client.WebhookEndpoints.DeleteAsync(endpointB.Id));
            }
        }

        // ---- events ----
        var events = await Check("events.list", null, () => client.Events.ListAsync(new EventListParams { Limit = 3 }));
        var eventId = events?.Data.FirstOrDefault()?.Id;
        if (eventId is not null)
        {
            await Check("events.retrieve", "event", () => client.Events.RetrieveAsync(eventId));
        }
        else
        {
            await Check("events.retrieve", "event", () => client.Events.RetrieveAsync("nbo000000000000evt"), NombaoneErrorCodes.ClientResourceNotFound);
        }

        await Check("events.catalog", null, () => client.Events.CatalogAsync());

        // ---- organization (+ billing) ----
        await Check("organization.retrieve", "organization", () => client.Organization.RetrieveAsync());
        await Check("organization.update", "organization", () => client.Organization.UpdateAsync(new TenantSettingsUpdateParams { Branding = new BrandingParams { DisplayName = "Verify Co" } }));
        await Check("organization.billing.retrieve", "billing_settings", () => client.Organization.Billing.RetrieveAsync());
        await Check("organization.billing.update", "billing_settings", () => client.Organization.Billing.UpdateAsync(new BillingSettingsUpdateParams { CommsEnabled = true }));

        // ---- metrics ----
        await Check("metrics.billing", "billing_metrics", () => client.Metrics.BillingAsync());

        // ---- report ----
        foreach (var line in r.Lines)
        {
            Console.WriteLine(line);
        }

        Console.WriteLine(new string('-', 78));
        Console.WriteLine($"{r.Total} methods | ok {r.Ok} | expected-errors {r.ExpectedErrors} | DEFECTS {r.Defects}");
        if (r.Defects > 0)
        {
            Environment.ExitCode = 1;
        }
    }
}
