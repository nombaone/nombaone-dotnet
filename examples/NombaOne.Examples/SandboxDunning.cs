using System;
using System.Threading.Tasks;
using NombaOne;

namespace NombaOne.Examples;

/// <summary>
/// Rehearse involuntary failure and recovery with a declining test card. A
/// declining card would 422 on the very first charge, so we defer that charge
/// with a trial and force the failure with the test clock instead.
/// </summary>
internal static class SandboxDunning
{
    internal static async Task RunAsync()
    {
        using var nombaone = new Nombaone();
        var tag = Guid.NewGuid().ToString("N").Substring(0, 8);

        var plan = await nombaone.Plans.CreateAsync(new PlanCreateParams { Name = $"Dunning {tag}" });
        var price = await nombaone.Plans.Prices.CreateAsync(plan.Id, new PriceCreateParams { UnitAmountInKobo = 250_000, Interval = "month" });
        var customer = await nombaone.Customers.CreateAsync(new CustomerCreateParams { Email = $"dun-{tag}@example.com", Name = "Dun Ning" });

        // A card that always declines like a thin balance does.
        var method = await nombaone.Sandbox.CreatePaymentMethodAsync(new SandboxPaymentMethodParams
        {
            CustomerId = customer.Id,
            Behavior = "decline_insufficient_funds",
        });

        // A trial defers the first charge, so creation succeeds…
        var sub = await nombaone.Subscriptions.CreateAsync(new SubscriptionCreateParams
        {
            CustomerId = customer.Id,
            PriceId = price.Id,
            PaymentMethodId = method.Id,
            TrialDays = 1,
        });
        Console.WriteLine($"subscribed {sub.Id} -> {sub.Status}");

        // …then the test clock runs the cycle and the charge fails.
        var cycle = await nombaone.Sandbox.AdvanceCycleAsync(sub.Id);
        Console.WriteLine($"advanced   outcome={cycle.Outcome}, invoice={cycle.Invoice.Id} status={cycle.Invoice.Status}");

        var dunning = await nombaone.Subscriptions.Dunning.RetrieveAsync(sub.Id);
        Console.WriteLine($"dunning    status={dunning.Status}, attempts={dunning.AttemptsUsed}/{dunning.MaxAttempts}");
        Console.WriteLine($"grace      access until {dunning.GraceAccessUntil?.ToString("o") ?? "(none)"} — 'past_due' is not 'canceled'");
        Console.WriteLine($"next retry {dunning.NextAttemptAt?.ToString("o") ?? "(none)"}");
    }
}
