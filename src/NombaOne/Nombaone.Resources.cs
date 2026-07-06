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

    /// <summary>Payment methods — cards, mandates, virtual accounts.</summary>
    public PaymentMethodsResource PaymentMethods { get; private set; } = null!;

    /// <summary>Direct-debit mandates (async NIBSS consent).</summary>
    public MandatesResource Mandates { get; private set; } = null!;

    /// <summary>Settlements, refunds, payouts, escrow.</summary>
    public SettlementsResource Settlements { get; private set; } = null!;

    /// <summary>Webhook endpoint management (REST). To verify deliveries, see <c>WebhookVerifier</c>.</summary>
    public WebhookEndpointsResource WebhookEndpoints { get; private set; } = null!;

    /// <summary>The domain-event log — your reconciliation backstop.</summary>
    public EventsResource Events { get; private set; } = null!;

    /// <summary>Organization settings + billing/dunning policy.</summary>
    public OrganizationResource Organization { get; private set; } = null!;

    /// <summary>Billing KPIs computed from the ledger.</summary>
    public MetricsResource Metrics { get; private set; } = null!;

    /// <summary>Sandbox-only simulation instruments (test clock, test methods, webhook simulate).</summary>
    public SandboxResource Sandbox { get; private set; } = null!;

    // Wired from the constructor. Extended as each resource namespace is added.
    partial void InitializeResources()
    {
        Customers = new CustomersResource(this);
        Plans = new PlansResource(this);
        Prices = new PricesResource(this);
        Subscriptions = new SubscriptionsResource(this);
        Invoices = new InvoicesResource(this);
        Coupons = new CouponsResource(this);
        PaymentMethods = new PaymentMethodsResource(this);
        Mandates = new MandatesResource(this);
        Settlements = new SettlementsResource(this);
        WebhookEndpoints = new WebhookEndpointsResource(this);
        Events = new EventsResource(this);
        Organization = new OrganizationResource(this);
        Metrics = new MetricsResource(this);
        Sandbox = new SandboxResource(this);
    }
}
