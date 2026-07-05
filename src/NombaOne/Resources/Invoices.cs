using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NombaOne;

/// <summary>
/// What a billing cycle produced. You never create invoices — subscription
/// cycles do; amounts are locked at finalization. All amounts are integer kobo.
/// </summary>
public sealed class Invoice : NombaoneEntity
{
    /// <summary>Always <c>"invoice"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "invoice";

    /// <summary>The invoice id (<c>nbo…inv</c>).</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>The customer billed (<c>nbo…cus</c>).</summary>
    [JsonPropertyName("customerId")]
    public string CustomerId { get; init; } = string.Empty;

    /// <summary>The subscription this invoice belongs to, or <c>null</c> for a manual invoice.</summary>
    [JsonPropertyName("subscriptionId")]
    public string? SubscriptionId { get; init; }

    /// <summary>One of <c>draft</c>, <c>open</c>, <c>partially_paid</c>, <c>paid</c>, <c>void</c>, <c>uncollectible</c>.</summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    /// <summary>One of <c>subscription_create</c>, <c>subscription_cycle</c>, <c>subscription_update</c>, <c>manual</c>.</summary>
    [JsonPropertyName("billingReason")]
    public string BillingReason { get; init; } = string.Empty;

    /// <summary>Sum of line items before discounts and credits, in integer kobo.</summary>
    [JsonPropertyName("subtotalInKobo")]
    public long SubtotalInKobo { get; init; }

    /// <summary>Total discount applied, in integer kobo.</summary>
    [JsonPropertyName("discountTotalInKobo")]
    public long DiscountTotalInKobo { get; init; }

    /// <summary>Total credit applied, in integer kobo.</summary>
    [JsonPropertyName("creditTotalInKobo")]
    public long CreditTotalInKobo { get; init; }

    /// <summary>The invoice total after discounts and credits, in integer kobo.</summary>
    [JsonPropertyName("totalInKobo")]
    public long TotalInKobo { get; init; }

    /// <summary>The amount due, in integer kobo.</summary>
    [JsonPropertyName("amountDueInKobo")]
    public long AmountDueInKobo { get; init; }

    /// <summary>The amount already paid, in integer kobo.</summary>
    [JsonPropertyName("amountPaidInKobo")]
    public long AmountPaidInKobo { get; init; }

    /// <summary>The amount still outstanding, in integer kobo.</summary>
    [JsonPropertyName("amountRemainingInKobo")]
    public long AmountRemainingInKobo { get; init; }

    /// <summary>Always <c>"NGN"</c>.</summary>
    [JsonPropertyName("currency")]
    public string Currency { get; init; } = "NGN";

    /// <summary>The billing period start, or <c>null</c>.</summary>
    [JsonPropertyName("periodStart")]
    public DateTimeOffset? PeriodStart { get; init; }

    /// <summary>The billing period end, or <c>null</c>.</summary>
    [JsonPropertyName("periodEnd")]
    public DateTimeOffset? PeriodEnd { get; init; }

    /// <summary>When payment is due, or <c>null</c>.</summary>
    [JsonPropertyName("dueDate")]
    public DateTimeOffset? DueDate { get; init; }

    /// <summary>The invoice line items.</summary>
    [JsonPropertyName("lineItems")]
    public IReadOnlyList<InvoiceLineItem> LineItems { get; init; } = Array.Empty<InvoiceLineItem>();

    /// <summary>When the invoice was finalized, or <c>null</c>.</summary>
    [JsonPropertyName("finalizedAt")]
    public DateTimeOffset? FinalizedAt { get; init; }

    /// <summary>When the invoice was paid, or <c>null</c>.</summary>
    [JsonPropertyName("paidAt")]
    public DateTimeOffset? PaidAt { get; init; }

    /// <summary>When the invoice was voided, or <c>null</c>.</summary>
    [JsonPropertyName("voidedAt")]
    public DateTimeOffset? VoidedAt { get; init; }

    /// <summary>The environment (<c>"sandbox"</c> or <c>"live"</c>).</summary>
    [JsonPropertyName("mode")]
    public string Mode { get; init; } = string.Empty;

    /// <summary>When the invoice was created.</summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>Filters for <see cref="InvoicesResource.ListAsync"/>.</summary>
public sealed class InvoiceListParams
{
    /// <summary>Filter to one customer (<c>nbo…cus</c>).</summary>
    public string? CustomerId { get; init; }

    /// <summary>Filter to one subscription (<c>nbo…sub</c>).</summary>
    public string? SubscriptionId { get; init; }

    /// <summary>
    /// Filter by status. The list filter accepts <c>draft</c>, <c>open</c>,
    /// <c>paid</c>, <c>void</c>, <c>uncollectible</c> — <b>not</b>
    /// <c>partially_paid</c> (though invoice objects can carry it).
    /// </summary>
    public string? Status { get; init; }

    /// <summary>Page size, 1–100 (the API default is 20).</summary>
    public int? Limit { get; init; }

    /// <summary>Opaque cursor from a previous page's <c>Pagination.NextCursor</c>.</summary>
    public string? Cursor { get; init; }
}

/// <summary>Parameters for <see cref="InvoicesResource.VoidAsync"/>.</summary>
public sealed class InvoiceVoidParams
{
    /// <summary>An optional comment recorded with the void.</summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; init; }
}

/// <summary>Invoices — read what the billing engine produced; void what should never be collected.</summary>
public sealed class InvoicesResource : NombaoneResource
{
    internal InvoicesResource(Nombaone client)
        : base(client)
    {
    }

    /// <summary>Retrieve an invoice by id.</summary>
    /// <param name="id">The invoice id (<c>nbo…inv</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="NotFoundException">404 <c>INVOICE_NOT_FOUND</c>.</exception>
    public Task<Invoice> RetrieveAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        GetAsync<Invoice>($"/invoices/{Seg(id)}", options, cancellationToken);

    /// <summary>List invoices, newest first.</summary>
    /// <param name="parameters">Optional filters and page size.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <example>
    /// <code>
    /// await foreach (var invoice in nombaone.Invoices.ListAutoPagingAsync(new InvoiceListParams { Status = "open" }))
    /// {
    ///     Console.WriteLine($"{invoice.Id} {invoice.AmountDueInKobo}");
    /// }
    /// </code>
    /// </example>
    public Task<NombaonePage<Invoice>> ListAsync(InvoiceListParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAsync<Invoice>("/invoices", BuildListQuery(parameters), options, cancellationToken);

    /// <summary>List invoices as an async stream, fetching pages for you as you iterate.</summary>
    /// <param name="parameters">Optional filters and page size.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Stops iteration and cancels in-flight fetches.</param>
    public IAsyncEnumerable<Invoice> ListAutoPagingAsync(InvoiceListParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAutoPagingAsync<Invoice>("/invoices", BuildListQuery(parameters), options, cancellationToken);

    /// <summary>
    /// Void an open, unpaid invoice. Paid invoices can't be voided — refund the
    /// settlement instead.
    /// </summary>
    /// <param name="id">The invoice id (<c>nbo…inv</c>).</param>
    /// <param name="parameters">An optional comment.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="ConflictException">409 <c>INVOICE_NOT_VOIDABLE</c> or <c>INVOICE_ALREADY_PAID</c>.</exception>
    public Task<Invoice> VoidAsync(string id, InvoiceVoidParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<Invoice>($"/invoices/{Seg(id)}/void", (object?)parameters ?? EmptyBody, options, cancellationToken);

    private static IReadOnlyDictionary<string, string?> BuildListQuery(InvoiceListParams? parameters) => new Dictionary<string, string?>
    {
        ["customerId"] = parameters?.CustomerId,
        ["subscriptionId"] = parameters?.SubscriptionId,
        ["status"] = parameters?.Status,
        ["limit"] = parameters?.Limit?.ToString(CultureInfo.InvariantCulture),
        ["cursor"] = parameters?.Cursor,
    };
}
