namespace NombaOne;

/// <summary>Resource namespaces exposed on the client.</summary>
public sealed partial class Nombaone
{
    /// <summary>Customers — the people and businesses you bill, plus their credit and discounts.</summary>
    public CustomersResource Customers { get; private set; } = null!;

    /// <summary>Plans — your catalog. Prices nest under <c>Plans.Prices</c>.</summary>
    public PlansResource Plans { get; private set; } = null!;

    /// <summary>Prices — immutable amounts and cadences (read + deactivate).</summary>
    public PricesResource Prices { get; private set; } = null!;

    /// <summary>Subscriptions — the core billing object, with <c>Schedule</c> and <c>Dunning</c> sub-namespaces.</summary>
    public SubscriptionsResource Subscriptions { get; private set; } = null!;

    /// <summary>Invoices — what billing cycles produced (read + void).</summary>
    public InvoicesResource Invoices { get; private set; } = null!;

    /// <summary>Coupons — reusable discount rules.</summary>
    public CouponsResource Coupons { get; private set; } = null!;

    // Wired from the constructor. Extended as each resource namespace is added.
    partial void InitializeResources()
    {
        Customers = new CustomersResource(this);
        Plans = new PlansResource(this);
        Prices = new PricesResource(this);
        Subscriptions = new SubscriptionsResource(this);
        Invoices = new InvoicesResource(this);
        Coupons = new CouponsResource(this);
    }
}
