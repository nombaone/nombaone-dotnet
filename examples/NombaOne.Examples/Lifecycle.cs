using System;
using System.Threading.Tasks;
using NombaOne;

namespace NombaOne.Examples;

/// <summary>The subscription lifecycle: subscribe, run a cycle, change, pause/resume, cancel.</summary>
internal static class Lifecycle
{
    internal static async Task RunAsync()
    {
        using var nombaone = new Nombaone();
        var tag = Guid.NewGuid().ToString("N").Substring(0, 8);

        var plan = await nombaone.Plans.CreateAsync(new PlanCreateParams { Name = $"Lifecycle {tag}" });
        var monthly = await nombaone.Plans.Prices.CreateAsync(plan.Id, new PriceCreateParams { UnitAmountInKobo = 250_000, Interval = "month" });
        var premium = await nombaone.Plans.Prices.CreateAsync(plan.Id, new PriceCreateParams { UnitAmountInKobo = 500_000, Interval = "month" });
        var customer = await nombaone.Customers.CreateAsync(new CustomerCreateParams { Email = $"life-{tag}@example.com", Name = "Life Cycle" });
        var method = await nombaone.Sandbox.CreatePaymentMethodAsync(new SandboxPaymentMethodParams { CustomerId = customer.Id });

        var sub = await nombaone.Subscriptions.CreateAsync(new SubscriptionCreateParams
        {
            CustomerId = customer.Id,
            PriceId = monthly.Id,
            PaymentMethodId = method.Id,
        });
        Console.WriteLine($"created    {sub.Id} -> {sub.Status}");

        var cycle = await nombaone.Sandbox.AdvanceCycleAsync(sub.Id);
        Console.WriteLine($"advanced   outcome={cycle.Outcome}, invoice={cycle.Invoice.Id} total={cycle.Invoice.TotalInKobo} kobo");

        var upcoming = await nombaone.Subscriptions.RetrieveUpcomingInvoiceAsync(sub.Id);
        Console.WriteLine($"upcoming   period {upcoming.PeriodIndex}: amountDue={upcoming.AmountDueInKobo} kobo");

        var changed = await nombaone.Subscriptions.ChangeAsync(sub.Id, new SubscriptionChangeParams { PriceId = premium.Id });
        Console.WriteLine($"upgraded   -> price {changed.PriceId}, status {changed.Status}");

        var paused = await nombaone.Subscriptions.PauseAsync(sub.Id);
        Console.WriteLine($"paused     -> {paused.Status}");

        var resumed = await nombaone.Subscriptions.ResumeAsync(sub.Id);
        Console.WriteLine($"resumed    -> {resumed.Status}");

        var canceled = await nombaone.Subscriptions.CancelAsync(sub.Id, new SubscriptionCancelParams { Mode = "at_period_end" });
        Console.WriteLine($"canceled   -> {canceled.Status}, cancelAtPeriodEnd={canceled.CancelAtPeriodEnd}");
    }
}
