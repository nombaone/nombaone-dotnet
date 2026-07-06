using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NombaOne;

/// <summary>What mandate creation hands back — consent is still pending at this point.</summary>
public sealed class MandateSetup : NombaoneEntity
{
    /// <summary>Always <c>"mandate_setup"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "mandate_setup";

    /// <summary>The payment-method reference this mandate will live under (<c>nbo…pmt</c>).</summary>
    [JsonPropertyName("reference")]
    public string Reference { get; init; } = string.Empty;

    /// <summary>The provider-side mandate reference.</summary>
    [JsonPropertyName("mandateRef")]
    public string MandateRef { get; init; } = string.Empty;

    /// <summary><c>consent_pending</c> until the customer's bank confirms.</summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    /// <summary>Human instructions to relay to the customer to authorize the debit.</summary>
    [JsonPropertyName("consentInstruction")]
    public string ConsentInstruction { get; init; } = string.Empty;
}

/// <summary>Parameters for <see cref="MandatesResource.CreateAsync"/>.</summary>
public sealed class MandateCreateParams
{
    /// <summary>The customer this mandate belongs to (<c>nbo…cus</c>).</summary>
    [JsonPropertyName("customerRef")]
    public required string CustomerRef { get; init; }

    /// <summary>The customer's bank account number.</summary>
    [JsonPropertyName("customerAccountNumber")]
    public required string CustomerAccountNumber { get; init; }

    /// <summary>CBN 3-digit bank code (058 GTB · 044 Access · 033 UBA · …).</summary>
    [JsonPropertyName("bankCode")]
    public required string BankCode { get; init; }

    /// <summary>The customer's name.</summary>
    [JsonPropertyName("customerName")]
    public required string CustomerName { get; init; }

    /// <summary>The name on the bank account.</summary>
    [JsonPropertyName("customerAccountName")]
    public required string CustomerAccountName { get; init; }

    /// <summary>The customer's phone number.</summary>
    [JsonPropertyName("customerPhoneNumber")]
    public required string CustomerPhoneNumber { get; init; }

    /// <summary>The customer's address.</summary>
    [JsonPropertyName("customerAddress")]
    public required string CustomerAddress { get; init; }

    /// <summary>Shown on the customer's statement.</summary>
    [JsonPropertyName("narration")]
    public required string Narration { get; init; }

    /// <summary>
    /// Hard per-debit ceiling, in integer kobo (₦1.00 = 100). Charges above it
    /// fail with <c>MANDATE_MAX_AMOUNT_EXCEEDED</c>.
    /// </summary>
    [JsonPropertyName("maxAmountInKobo")]
    public required long MaxAmountInKobo { get; init; }

    /// <summary>
    /// Debit cadence. One of <c>variable</c>, <c>weekly</c>, <c>every_two_weeks</c>,
    /// <c>monthly</c>, <c>every_two_months</c>, <c>every_three_months</c>,
    /// <c>every_four_months</c>, <c>every_six_months</c>, <c>every_twelve_months</c>.
    /// Defaults to <c>monthly</c> server-side.
    /// </summary>
    [JsonPropertyName("frequency")]
    public string? Frequency { get; init; }

    /// <summary>Local date-time (no zone). Defaults to tomorrow server-side.</summary>
    [JsonPropertyName("startDate")]
    public string? StartDate { get; init; }

    /// <summary>Local date-time (no zone). Defaults to one year out server-side.</summary>
    [JsonPropertyName("endDate")]
    public string? EndDate { get; init; }
}

/// <summary>
/// Direct-debit mandates (NIBSS). Creation is <b>asynchronous</b>: the mandate
/// starts <c>consent_pending</c> and activates only after the customer authorizes
/// it with their bank — the engine sweeps for activation and fires
/// <c>payment_method.attached</c>/<c>payment_method.updated</c>. Don't poll; listen
/// for the webhook, and don't charge before it's active
/// (<c>MANDATE_NOT_ACTIVE</c> / <c>MANDATE_CONSENT_PENDING</c>).
/// </summary>
public sealed class MandatesResource : NombaoneResource
{
    internal MandatesResource(Nombaone client)
        : base(client)
    {
    }

    /// <summary>
    /// Create a mandate. This moves money-shaped state, so the SDK sends an
    /// <c>Idempotency-Key</c> automatically.
    /// </summary>
    /// <param name="parameters">The mandate details (max amount is integer kobo).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <example>
    /// <code>
    /// var mandate = await nombaone.Mandates.CreateAsync(new MandateCreateParams
    /// {
    ///     CustomerRef = customer.Id,
    ///     CustomerAccountNumber = "0123456789",
    ///     BankCode = "058",
    ///     CustomerName = "Ada Lovelace",
    ///     CustomerAccountName = "Ada Lovelace",
    ///     CustomerPhoneNumber = "+2348012345678",
    ///     CustomerAddress = "1 Marina, Lagos",
    ///     Narration = "Acme Pro subscription",
    ///     MaxAmountInKobo = 500_000, // ₦5,000 ceiling per debit
    /// });
    /// // relay mandate.ConsentInstruction to the customer, then wait for the webhook
    /// </code>
    /// </example>
    public Task<MandateSetup> CreateAsync(MandateCreateParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<MandateSetup>("/mandates", parameters, options, cancellationToken);

    /// <summary>
    /// Check a mandate's current standing. Returns the underlying
    /// <see cref="PaymentMethod"/> row (its status moves <c>consent_pending</c> → <c>active</c>).
    /// </summary>
    /// <param name="id">The mandate / payment-method id (<c>nbo…pmt</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<PaymentMethod> RetrieveAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        GetAsync<PaymentMethod>($"/mandates/{Seg(id)}", options, cancellationToken);
}
