namespace NombaOne;

/// <summary>Resource namespaces exposed on the client.</summary>
public sealed partial class Nombaone
{
    /// <summary>Customers — the people and businesses you bill, plus their credit and discounts.</summary>
    public CustomersResource Customers { get; private set; } = null!;

    // Wired from the constructor. Extended as each resource namespace is added.
    partial void InitializeResources()
    {
        Customers = new CustomersResource(this);
    }
}
