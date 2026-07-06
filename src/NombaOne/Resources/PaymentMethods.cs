using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NombaOne;

/// <summary>
/// How a customer pays. Card and mandate are <b>pull</b> rails (the engine
/// initiates the debit); a virtual account is the <b>push</b> rail (the customer
/// sends a transfer and the engine matches it). Never contains a PAN or token.
/// </summary>
public sealed class PaymentMethod : NombaoneEntity
{
    /// <summary>Always <c>"payment_method"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "payment_method";

    /// <summary>The payment-method id (<c>nbo…pmt</c>).</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>The owning customer (<c>nbo…cus</c>).</summary>
    [JsonPropertyName("customerId")]
    public string CustomerId { get; init; } = string.Empty;

    /// <summary>One of <c>card</c>, <c>mandate</c>, <c>virtual_account</c>.</summary>
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = string.Empty;

    /// <summary>One of <c>setup_pending</c>, <c>consent_pending</c>, <c>active</c>, <c>removed</c>, <c>expired</c>.</summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    /// <summary>Whether this is the customer's default payment method.</summary>
    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; init; }

    /// <summary>The card brand, or <c>null</c>.</summary>
    [JsonPropertyName("brand")]
    public string? Brand { get; init; }

    /// <summary>The card's last four digits, or <c>null</c>.</summary>
    [JsonPropertyName("last4")]
    public string? Last4 { get; init; }

    /// <summary>The card expiry month, or <c>null</c>.</summary>
    [JsonPropertyName("expMonth")]
    public int? ExpMonth { get; init; }

    /// <summary>The card expiry year, or <c>null</c>.</summary>
    [JsonPropertyName("expYear")]
    public int? ExpYear { get; init; }

    /// <summary>The environment (<c>"sandbox"</c> or <c>"live"</c>).</summary>
    [JsonPropertyName("mode")]
    public string Mode { get; init; } = string.Empty;

    /// <summary>When the payment method was created.</summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>When the payment method was last updated.</summary>
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>A hosted-checkout handoff: send the customer to <see cref="CheckoutLink"/>.</summary>
public sealed class CheckoutSetup : NombaoneEntity
{
    /// <summary>Always <c>"checkout_setup"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "checkout_setup";

    /// <summary>The setup reference.</summary>
    [JsonPropertyName("reference")]
    public string Reference { get; init; } = string.Empty;

    /// <summary>The PCI-scoped hosted page where the customer enters their card.</summary>
    [JsonPropertyName("checkoutLink")]
    public string CheckoutLink { get; init; } = string.Empty;
}

/// <summary>A dedicated NUBAN the customer pushes transfers to.</summary>
public sealed class VirtualAccount : NombaoneEntity
{
    /// <summary>Always <c>"virtual_account"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "virtual_account";

    /// <summary>The account reference.</summary>
    [JsonPropertyName("reference")]
    public string Reference { get; init; } = string.Empty;

    /// <summary>The bank the account sits at.</summary>
    [JsonPropertyName("bankName")]
    public string BankName { get; init; } = string.Empty;

    /// <summary>The NUBAN account number.</summary>
    [JsonPropertyName("accountNumber")]
    public string AccountNumber { get; init; } = string.Empty;

    /// <summary>The account holder name.</summary>
    [JsonPropertyName("accountName")]
    public string AccountName { get; init; } = string.Empty;

    /// <summary>The internal account reference.</summary>
    [JsonPropertyName("accountRef")]
    public string AccountRef { get; init; } = string.Empty;
}

/// <summary>Parameters for <see cref="PaymentMethodsResource.SetupAsync"/>.</summary>
public sealed class PaymentMethodSetupParams
{
    /// <summary>The customer this card will belong to (<c>nbo…cus</c>).</summary>
    [JsonPropertyName("customerRef")]
    public required string CustomerRef { get; init; }

    /// <summary>The validation charge, in integer kobo (₦1.00 = 100).</summary>
    [JsonPropertyName("amountInKobo")]
    public required long AmountInKobo { get; init; }

    /// <summary>Where the hosted checkout returns the customer afterwards.</summary>
    [JsonPropertyName("callbackUrl")]
    public required string CallbackUrl { get; init; }
}

/// <summary>Parameters for <see cref="PaymentMethodsResource.CreateVirtualAccountAsync"/>.</summary>
public sealed class PaymentMethodVirtualAccountParams
{
    /// <summary>The customer to issue the account for (<c>nbo…cus</c>).</summary>
    [JsonPropertyName("customerRef")]
    public required string CustomerRef { get; init; }

    /// <summary>Optional expected amount hint, in integer kobo.</summary>
    [JsonPropertyName("expectedAmount")]
    public long? ExpectedAmount { get; init; }

    /// <summary>Optional ISO date the account should expire.</summary>
    [JsonPropertyName("expiryDate")]
    public string? ExpiryDate { get; init; }
}

/// <summary>Filters for <see cref="PaymentMethodsResource.ListAsync"/>.</summary>
public sealed class PaymentMethodListParams
{
    /// <summary>Filter to one customer (<c>nbo…cus</c>). Note the wire name is <c>customerRef</c>.</summary>
    public string? CustomerRef { get; init; }

    /// <summary>Page size, 1–100 (the API default is 20).</summary>
    public int? Limit { get; init; }

    /// <summary>Opaque cursor from a previous page's <c>Pagination.NextCursor</c>.</summary>
    public string? Cursor { get; init; }
}

/// <summary>
/// Payment methods — cards (via hosted checkout), direct-debit mandates (see
/// <c>nombaone.Mandates</c>), and virtual accounts for the transfer rail.
/// </summary>
public sealed class PaymentMethodsResource : NombaoneResource
{
    internal PaymentMethodsResource(Nombaone client)
        : base(client)
    {
    }

    /// <summary>
    /// Start a hosted-checkout card capture. Card entry happens on the PCI
    /// hosted page — no card data ever touches your servers. The method appears
    /// as <c>setup_pending</c> until the customer completes checkout.
    /// </summary>
    /// <param name="parameters">The customer, validation amount (kobo), and callback URL.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <example>
    /// <code>
    /// var setup = await nombaone.PaymentMethods.SetupAsync(new PaymentMethodSetupParams
    /// {
    ///     CustomerRef = customer.Id,
    ///     AmountInKobo = 5_000, // ₦50 validation charge
    ///     CallbackUrl = "https://example.com/billing/return",
    /// });
    /// // redirect the customer to setup.CheckoutLink
    /// </code>
    /// </example>
    public Task<CheckoutSetup> SetupAsync(PaymentMethodSetupParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<CheckoutSetup>("/payment-methods/setup", parameters, options, cancellationToken);

    /// <summary>
    /// Issue a dedicated virtual account (NUBAN) so the customer can pay by bank
    /// transfer. The engine matches inbound transfers to invoices by reference
    /// and exact integer-kobo amount.
    /// </summary>
    /// <param name="parameters">The customer and optional hints.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<VirtualAccount> CreateVirtualAccountAsync(PaymentMethodVirtualAccountParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<VirtualAccount>("/payment-methods/virtual-account", parameters, options, cancellationToken);

    /// <summary>Retrieve a payment method by id.</summary>
    /// <param name="id">The payment-method id (<c>nbo…pmt</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="NotFoundException">404 <c>PAYMENT_METHOD_NOT_FOUND</c>.</exception>
    public Task<PaymentMethod> RetrieveAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        GetAsync<PaymentMethod>($"/payment-methods/{Seg(id)}", options, cancellationToken);

    /// <summary>List payment methods, newest first.</summary>
    /// <param name="parameters">Optional filters and page size.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<NombaonePage<PaymentMethod>> ListAsync(PaymentMethodListParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAsync<PaymentMethod>("/payment-methods", BuildListQuery(parameters), options, cancellationToken);

    /// <summary>List payment methods as an async stream, fetching pages for you as you iterate.</summary>
    /// <param name="parameters">Optional filters and page size.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Stops iteration and cancels in-flight fetches.</param>
    public IAsyncEnumerable<PaymentMethod> ListAutoPagingAsync(PaymentMethodListParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAutoPagingAsync<PaymentMethod>("/payment-methods", BuildListQuery(parameters), options, cancellationToken);

    /// <summary>Make this the customer's default payment method.</summary>
    /// <param name="id">The payment-method id (<c>nbo…pmt</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<PaymentMethod> SetDefaultAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<PaymentMethod>($"/payment-methods/{Seg(id)}/default", EmptyBody, options, cancellationToken);

    /// <summary>
    /// Detach a payment method. Subscriptions still billing against it will need
    /// a replacement (<c>SUBSCRIPTION_PAYMENT_METHOD_REQUIRED</c> at next charge otherwise).
    /// </summary>
    /// <param name="id">The payment-method id (<c>nbo…pmt</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<PaymentMethod> RemoveAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        DeleteAsync<PaymentMethod>($"/payment-methods/{Seg(id)}", options, cancellationToken);

    private static IReadOnlyDictionary<string, string?> BuildListQuery(PaymentMethodListParams? parameters) => new Dictionary<string, string?>
    {
        ["customerRef"] = parameters?.CustomerRef,
        ["limit"] = parameters?.Limit?.ToString(CultureInfo.InvariantCulture),
        ["cursor"] = parameters?.Cursor,
    };
}
