using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NombaOne;

/// <summary>A subscriber — the person or business you bill.</summary>
public sealed class Customer : NombaoneEntity
{
    /// <summary>Always <c>"customer"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "customer";

    /// <summary>The customer id (<c>nbo…cus</c>).</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>Unique within your organization and environment.</summary>
    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    /// <summary>The customer's display name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>The customer's phone number, or <c>null</c>.</summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; init; }

    /// <summary>Free-form annotations you attached (arbitrary JSON values).</summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, JsonElement>? Metadata { get; init; }

    /// <summary>The environment (<c>"sandbox"</c> or <c>"live"</c>).</summary>
    [JsonPropertyName("mode")]
    public string Mode { get; init; } = string.Empty;

    /// <summary>When the customer was created.</summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>When the customer was last updated.</summary>
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>A grant of credit that future invoices draw down before charging any rail.</summary>
public sealed class CreditGrant : NombaoneEntity
{
    /// <summary>Always <c>"credit_grant"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "credit_grant";

    /// <summary>The credit-grant id (<c>nbo…crg</c>).</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>The customer the credit belongs to (<c>nbo…cus</c>).</summary>
    [JsonPropertyName("customerId")]
    public string CustomerId { get; init; } = string.Empty;

    /// <summary>The original granted amount, in integer kobo (₦1.00 = 100).</summary>
    [JsonPropertyName("amountInKobo")]
    public long AmountInKobo { get; init; }

    /// <summary>What is left to consume, in integer kobo.</summary>
    [JsonPropertyName("remainingInKobo")]
    public long RemainingInKobo { get; init; }

    /// <summary>One of <c>downgrade_proration</c>, <c>manual</c>, <c>goodwill</c>, <c>coupon</c>.</summary>
    [JsonPropertyName("source")]
    public string Source { get; init; } = string.Empty;

    /// <summary>Your own reference for this grant, or <c>null</c>.</summary>
    [JsonPropertyName("sourceReference")]
    public string? SourceReference { get; init; }

    /// <summary>The environment (<c>"sandbox"</c> or <c>"live"</c>).</summary>
    [JsonPropertyName("mode")]
    public string Mode { get; init; } = string.Empty;

    /// <summary>When the grant was voided, or <c>null</c> if still active.</summary>
    [JsonPropertyName("voidedAt")]
    public DateTimeOffset? VoidedAt { get; init; }

    /// <summary>When the grant was created.</summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>A customer's live credit position: total balance plus the grants behind it.</summary>
public sealed class CreditBalance : NombaoneEntity
{
    /// <summary>Always <c>"credit_balance"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "credit_balance";

    /// <summary>The customer this balance belongs to (<c>nbo…cus</c>).</summary>
    [JsonPropertyName("customerId")]
    public string CustomerId { get; init; } = string.Empty;

    /// <summary>Sum of remaining credit across active grants, in integer kobo.</summary>
    [JsonPropertyName("balanceInKobo")]
    public long BalanceInKobo { get; init; }

    /// <summary>The grants that make up the balance.</summary>
    [JsonPropertyName("grants")]
    public IReadOnlyList<CreditGrant> Grants { get; init; } = Array.Empty<CreditGrant>();
}

/// <summary>Parameters for <see cref="CustomersResource.CreateAsync"/>.</summary>
public sealed class CustomerCreateParams
{
    /// <summary>Unique per organization + environment (<c>CUSTOMER_EMAIL_TAKEN</c> on reuse).</summary>
    [JsonPropertyName("email")]
    public required string Email { get; init; }

    /// <summary>The customer's display name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>The customer's phone number (optional).</summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; init; }

    /// <summary>Free-form annotations (arbitrary JSON values).</summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }
}

/// <summary>Parameters for <see cref="CustomersResource.UpdateAsync"/>. At least one field must be provided.</summary>
public sealed class CustomerUpdateParams
{
    /// <summary>A new display name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// A new phone number. Assign a value to set it, or
    /// <c>Optional&lt;string&gt;.Null</c> to clear it; leave unset to keep it.
    /// </summary>
    [JsonPropertyName("phone")]
    public Optional<string>? Phone { get; init; }

    /// <summary>Free-form annotations (arbitrary JSON values).</summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }
}

/// <summary>Filters for <see cref="CustomersResource.ListAsync"/>.</summary>
public sealed class CustomerListParams
{
    /// <summary>Exact-match filter on email.</summary>
    public string? Email { get; init; }

    /// <summary>Page size, 1–100 (the API default is 20).</summary>
    public int? Limit { get; init; }

    /// <summary>Opaque cursor from a previous page's <c>Pagination.NextCursor</c>.</summary>
    public string? Cursor { get; init; }
}

/// <summary>Parameters for <see cref="CustomersResource.GrantCreditAsync"/>.</summary>
public sealed class CustomerGrantCreditParams
{
    /// <summary>
    /// Amount to grant, in integer kobo (₦1.00 = 100). <c>250_000</c> is ₦2,500 —
    /// not ₦250,000. Multiply naira by 100 exactly once, at the edge of your system.
    /// </summary>
    [JsonPropertyName("amountInKobo")]
    public required long AmountInKobo { get; init; }

    /// <summary>Defaults to <c>manual</c> server-side; may also be <c>goodwill</c>.</summary>
    [JsonPropertyName("source")]
    public string? Source { get; init; }

    /// <summary>Your own reference for this grant (support ticket, promo id, …).</summary>
    [JsonPropertyName("sourceReference")]
    public string? SourceReference { get; init; }

    /// <summary>Free-form annotations (arbitrary JSON values).</summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }
}

/// <summary>Parameters for <see cref="CustomersResource.ApplyDiscountAsync"/>.</summary>
public sealed class CustomerApplyDiscountParams
{
    /// <summary>A coupon id (<c>nbo…cpn</c>) or its code (e.g. <c>LAUNCH20</c>).</summary>
    [JsonPropertyName("coupon")]
    public required string Coupon { get; init; }
}

/// <summary>
/// Customers — the people and businesses you bill, plus their credit and
/// discounts. Reached via <c>nombaone.Customers</c>.
/// </summary>
/// <example>
/// <code>
/// var customer = await nombaone.Customers.CreateAsync(new CustomerCreateParams
/// {
///     Email = "ada@example.com",
///     Name = "Ada Lovelace",
/// });
/// </code>
/// </example>
public sealed class CustomersResource : NombaoneResource
{
    internal CustomersResource(Nombaone client)
        : base(client)
    {
    }

    /// <summary>
    /// Create a customer.
    /// </summary>
    /// <param name="parameters">The customer to create.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="ValidationException">422 <c>CLIENT_VALIDATION_FAILED</c> — see <c>Fields</c>.</exception>
    /// <exception cref="ConflictException">409 <c>CUSTOMER_EMAIL_TAKEN</c> — reuse the existing customer instead.</exception>
    /// <example>
    /// <code>
    /// var customer = await nombaone.Customers.CreateAsync(new CustomerCreateParams
    /// {
    ///     Email = "ada@example.com",
    ///     Name = "Ada Lovelace",
    ///     Metadata = new Dictionary&lt;string, object?&gt; { ["crmId"] = "crm_812" },
    /// });
    /// Console.WriteLine(customer.Id); // "nbo…cus"
    /// </code>
    /// </example>
    public Task<Customer> CreateAsync(CustomerCreateParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<Customer>("/customers", parameters, options, cancellationToken);

    /// <summary>
    /// Retrieve a customer by id.
    /// </summary>
    /// <param name="id">The customer id (<c>nbo…cus</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="NotFoundException">404 <c>CUSTOMER_NOT_FOUND</c> — check the id and that your key matches the environment.</exception>
    public Task<Customer> RetrieveAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        GetAsync<Customer>($"/customers/{Seg(id)}", options, cancellationToken);

    /// <summary>
    /// Update a customer's mutable fields. At least one field is required.
    /// </summary>
    /// <param name="id">The customer id (<c>nbo…cus</c>).</param>
    /// <param name="parameters">The fields to change.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <example>
    /// <code>
    /// await nombaone.Customers.UpdateAsync(customer.Id, new CustomerUpdateParams { Phone = "+2348012345678" });
    /// </code>
    /// </example>
    public Task<Customer> UpdateAsync(string id, CustomerUpdateParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PatchAsync<Customer>($"/customers/{Seg(id)}", parameters, options, cancellationToken);

    /// <summary>
    /// List customers, newest first.
    /// </summary>
    /// <param name="parameters">Optional filters and page size.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<NombaonePage<Customer>> ListAsync(CustomerListParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAsync<Customer>("/customers", BuildListQuery(parameters), options, cancellationToken);

    /// <summary>
    /// List customers as an async stream, fetching pages for you as you iterate.
    /// </summary>
    /// <param name="parameters">Optional filters and page size.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Stops iteration and cancels in-flight fetches.</param>
    /// <example>
    /// <code>
    /// await foreach (var customer in nombaone.Customers.ListAutoPagingAsync())
    /// {
    ///     Console.WriteLine(customer.Email);
    /// }
    /// </code>
    /// </example>
    public IAsyncEnumerable<Customer> ListAutoPagingAsync(CustomerListParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAutoPagingAsync<Customer>("/customers", BuildListQuery(parameters), options, cancellationToken);

    /// <summary>
    /// Apply a coupon to a customer. The resulting discount shapes every future
    /// invoice for the customer until it ends or is removed.
    /// </summary>
    /// <param name="id">The customer id (<c>nbo…cus</c>).</param>
    /// <param name="parameters">The coupon to apply.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="NotFoundException">404 <c>COUPON_NOT_FOUND</c>.</exception>
    /// <exception cref="ConflictException">409 <c>COUPON_ALREADY_APPLIED</c>.</exception>
    /// <example>
    /// <code>
    /// var discount = await nombaone.Customers.ApplyDiscountAsync(customer.Id, new CustomerApplyDiscountParams { Coupon = "LAUNCH20" });
    /// </code>
    /// </example>
    public Task<Discount> ApplyDiscountAsync(string id, CustomerApplyDiscountParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<Discount>($"/customers/{Seg(id)}/discount", parameters, options, cancellationToken);

    /// <summary>
    /// Remove the customer's active discount. Returns the ended discount.
    /// </summary>
    /// <param name="id">The customer id (<c>nbo…cus</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="NotFoundException">404 <c>DISCOUNT_NOT_FOUND</c>.</exception>
    public Task<Discount> RemoveDiscountAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        DeleteAsync<Discount>($"/customers/{Seg(id)}/discount", options, cancellationToken);

    /// <summary>
    /// Grant credit to a customer. Credit is drawn down oldest-grant-first by
    /// future invoices <b>before</b> any payment rail is charged. This endpoint
    /// moves money-shaped state, so the SDK sends an <c>Idempotency-Key</c>
    /// automatically (pass <c>options.IdempotencyKey</c> to control it across
    /// process restarts).
    /// </summary>
    /// <param name="id">The customer id (<c>nbo…cus</c>).</param>
    /// <param name="parameters">The amount (in kobo) and source.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <example>
    /// <code>
    /// await nombaone.Customers.GrantCreditAsync(customer.Id, new CustomerGrantCreditParams
    /// {
    ///     AmountInKobo = 250_000, // ₦2,500.00
    ///     Source = "goodwill",
    /// });
    /// </code>
    /// </example>
    public Task<CreditGrant> GrantCreditAsync(string id, CustomerGrantCreditParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<CreditGrant>($"/customers/{Seg(id)}/credit", parameters, options, cancellationToken);

    /// <summary>
    /// Retrieve the customer's credit balance and the grants behind it.
    /// </summary>
    /// <param name="id">The customer id (<c>nbo…cus</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<CreditBalance> RetrieveCreditBalanceAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        GetAsync<CreditBalance>($"/customers/{Seg(id)}/credit", options, cancellationToken);

    /// <summary>
    /// Void a credit grant — its remaining balance becomes unusable. Consumed
    /// credit is untouched.
    /// </summary>
    /// <param name="id">The customer id (<c>nbo…cus</c>).</param>
    /// <param name="grantId">The credit-grant id (<c>nbo…crg</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="ConflictException">409 <c>CREDIT_GRANT_ALREADY_VOIDED</c>.</exception>
    public Task<CreditGrant> VoidCreditAsync(string id, string grantId, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        DeleteAsync<CreditGrant>($"/customers/{Seg(id)}/credit/{Seg(grantId)}", options, cancellationToken);

    private static IReadOnlyDictionary<string, string?> BuildListQuery(CustomerListParams? parameters) => new Dictionary<string, string?>
    {
        ["email"] = parameters?.Email,
        ["limit"] = parameters?.Limit?.ToString(CultureInfo.InvariantCulture),
        ["cursor"] = parameters?.Cursor,
    };
}
