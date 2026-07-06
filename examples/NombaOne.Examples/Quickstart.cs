using System;
using System.Threading.Tasks;
using NombaOne;

namespace NombaOne.Examples;

/// <summary>Key → plan → price → customer → sandbox card → active subscription.</summary>
internal static class Quickstart
{
    internal static async Task RunAsync()
    {
        using var nombaone = new Nombaone(); // reads NOMBAONE_API_KEY
        Console.WriteLine($"Talking to {nombaone.BaseUrl} (mode: {nombaone.Mode})");

        var tag = Guid.NewGuid().ToString("N").Substring(0, 8);

        var plan = await nombaone.Plans.CreateAsync(new PlanCreateParams { Name = $"Pro {tag}" });
        var price = await nombaone.Plans.Prices.CreateAsync(plan.Id, new PriceCreateParams
        {
            UnitAmountInKobo = 250_000, // ₦2,500.00 per month
            Interval = "month",
        });
        var customer = await nombaone.Customers.CreateAsync(new CustomerCreateParams
        {
            Email = $"ada-{tag}@example.com",
            Name = "Ada Lovelace",
        });

        // Sandbox: mint a deterministic test card, then subscribe.
        var method = await nombaone.Sandbox.CreatePaymentMethodAsync(new SandboxPaymentMethodParams { CustomerId = customer.Id });
        var subscription = await nombaone.Subscriptions.CreateAsync(new SubscriptionCreateParams
        {
            CustomerId = customer.Id,
            PriceId = price.Id,
            PaymentMethodId = method.Id,
        });

        Console.WriteLine($"plan       {plan.Id}");
        Console.WriteLine($"price      {price.Id} ({price.UnitAmountInKobo} kobo / {price.Interval})");
        Console.WriteLine($"customer   {customer.Id}");
        Console.WriteLine($"method     {method.Id} ({method.Kind}, {method.Status})");
        Console.WriteLine($"subscription {subscription.Id} -> {subscription.Status}");
    }
}
