using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NombaOne;

/// <summary>Parameters for <see cref="SandboxResource.CreatePaymentMethodAsync"/>.</summary>
public sealed class SandboxPaymentMethodParams
{
    /// <summary>The customer to attach the test method to (<c>nbo…cus</c>).</summary>
    [JsonPropertyName("customerId")]
    public required string CustomerId { get; init; }

    /// <summary>
    /// The deterministic charge outcome. One of <c>success</c>,
    /// <c>decline_insufficient_funds</c>, <c>decline_expired_card</c>,
    /// <c>decline_do_not_honor</c>, <c>requires_otp</c>. Defaults to <c>success</c> server-side.
    /// </summary>
    [JsonPropertyName("behavior")]
    public string? Behavior { get; init; }

    /// <summary>Defaults to <c>card</c> server-side; <c>mandate</c> simulates silent direct debit.</summary>
    [JsonPropertyName("kind")]
    public string? Kind { get; init; }
}

/// <summary>What forcing one billing cycle produced.</summary>
public sealed class AdvanceCycleResult : NombaoneEntity
{
    /// <summary>Always <c>"advance_cycle_result"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "advance_cycle_result";

    /// <summary>The subscription that was advanced (<c>nbo…sub</c>).</summary>
    [JsonPropertyName("subscriptionId")]
    public string SubscriptionId { get; init; } = string.Empty;

    /// <summary>The cycle's billing outcome: <c>paid</c> | <c>past_due</c> | <c>pending</c> | <c>open</c>.</summary>
    [JsonPropertyName("outcome")]
    public string Outcome { get; init; } = string.Empty;

    /// <summary>The invoice the cycle produced (or the existing one if already billed).</summary>
    [JsonPropertyName("invoice")]
    public Invoice Invoice { get; init; } = new();
}

/// <summary>Parameters for <see cref="SandboxResource.SimulateWebhookAsync"/>.</summary>
public sealed class SandboxSimulateWebhookParams
{
    /// <summary>Any catalog event type, e.g. <c>invoice.payment_failed</c>.</summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>Shapes the delivery's <c>data</c> object.</summary>
    [JsonPropertyName("payload")]
    public IReadOnlyDictionary<string, object?>? Payload { get; init; }
}

/// <summary>The minted event and how many endpoint deliveries fired.</summary>
public sealed class WebhookSimulation : NombaoneEntity
{
    /// <summary>Always <c>"webhook_simulation"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "webhook_simulation";

    /// <summary>The emitted event's reference (<c>nbo…evt</c>).</summary>
    [JsonPropertyName("event")]
    public string Event { get; init; } = string.Empty;

    /// <summary>The emitted event type.</summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    /// <summary>How many endpoint deliveries fired.</summary>
    [JsonPropertyName("deliveredCount")]
    public int DeliveredCount { get; init; }
}

/// <summary>
/// <b>Sandbox only.</b> Simulation instruments that make billing outcomes happen
/// on demand — no cron waits, no real cards. These endpoints exist only on the
/// sandbox deployment; calling them with a live key throws locally, before any
/// network request.
/// </summary>
public sealed class SandboxResource : NombaoneResource
{
    internal SandboxResource(Nombaone client)
        : base(client)
    {
    }

    /// <summary>
    /// <b>Sandbox only.</b> Mint a ready, chargeable test payment method whose
    /// <c>Behavior</c> decides every future charge outcome deterministically.
    /// </summary>
    /// <param name="parameters">The customer and desired behavior.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="NombaoneException">Thrown locally when the client uses a live key.</exception>
    /// <example>
    /// <code>
    /// var method = await nombaone.Sandbox.CreatePaymentMethodAsync(new SandboxPaymentMethodParams
    /// {
    ///     CustomerId = customer.Id,
    ///     Behavior = "decline_insufficient_funds", // rehearse thin-balance dunning
    /// });
    /// </code>
    /// </example>
    public Task<PaymentMethod> CreatePaymentMethodAsync(SandboxPaymentMethodParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        AssertSandbox();
        return PostAsync<PaymentMethod>("/sandbox/payment-methods", parameters, options, cancellationToken);
    }

    /// <summary>
    /// <b>Sandbox only.</b> The test clock: run the subscription's next billing
    /// cycle right now, through the real engine — invoice, charge, ledger,
    /// webhooks and all.
    /// </summary>
    /// <param name="subscriptionId">The subscription id (<c>nbo…sub</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="NombaoneException">Thrown locally when the client uses a live key.</exception>
    /// <example>
    /// <code>
    /// var result = await nombaone.Sandbox.AdvanceCycleAsync(subscription.Id);
    /// Console.WriteLine(result.Outcome);             // "paid"
    /// Console.WriteLine(result.Invoice.TotalInKobo); // the real invoice it produced
    /// </code>
    /// </example>
    public Task<AdvanceCycleResult> AdvanceCycleAsync(string subscriptionId, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        AssertSandbox();
        return PostAsync<AdvanceCycleResult>($"/sandbox/subscriptions/{Seg(subscriptionId)}/advance-cycle", EmptyBody, options, cancellationToken);
    }

    /// <summary>
    /// <b>Sandbox only.</b> Emit a real, signed catalog event to your registered
    /// endpoints — the genuine pipeline (real secret, real signature, real
    /// retries), not a mock. The sandbox sends no organic webhooks; this is how
    /// you rehearse your handler.
    /// </summary>
    /// <param name="parameters">The event type and optional payload.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="NombaoneException">Thrown locally when the client uses a live key.</exception>
    /// <example>
    /// <code>
    /// await nombaone.Sandbox.SimulateWebhookAsync(new SandboxSimulateWebhookParams
    /// {
    ///     Type = "invoice.payment_failed",
    ///     Payload = new Dictionary&lt;string, object?&gt; { ["reference"] = invoice.Id, ["reason"] = "insufficient_funds" },
    /// });
    /// </code>
    /// </example>
    public Task<WebhookSimulation> SimulateWebhookAsync(SandboxSimulateWebhookParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        AssertSandbox();
        return PostAsync<WebhookSimulation>("/sandbox/webhooks/simulate", parameters, options, cancellationToken);
    }

    private void AssertSandbox()
    {
        if (Client.Mode == "live")
        {
            throw new NombaoneException(
                "nombaone.Sandbox.* only works with a sandbox key (nbo_sandbox_…) — the /v1/sandbox endpoints " +
                "do not exist on the live API. Use your sandbox key to rehearse, then go live without the sandbox calls.");
        }
    }
}
