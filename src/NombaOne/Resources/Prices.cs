using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NombaOne;

/// <summary>
/// How much a plan costs per billing interval. Prices are <b>immutable</b> once
/// created — to change pricing, create a new price and deactivate the old one.
/// Existing subscriptions keep the price they were sold at.
/// </summary>
public sealed class Price : NombaoneEntity
{
    /// <summary>Always <c>"price"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "price";

    /// <summary>The price id (<c>nbo…prc</c>).</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>The plan this price belongs to (<c>nbo…pln</c>).</summary>
    [JsonPropertyName("planId")]
    public string PlanId { get; init; } = string.Empty;

    /// <summary>Amount per unit per interval, in integer kobo (₦1.00 = 100).</summary>
    [JsonPropertyName("unitAmountInKobo")]
    public long UnitAmountInKobo { get; init; }

    /// <summary>Always <c>"NGN"</c>.</summary>
    [JsonPropertyName("currency")]
    public string Currency { get; init; } = "NGN";

    /// <summary>One of <c>day</c>, <c>week</c>, <c>month</c>, <c>year</c>.</summary>
    [JsonPropertyName("interval")]
    public string Interval { get; init; } = string.Empty;

    /// <summary>Bill every N intervals.</summary>
    [JsonPropertyName("intervalCount")]
    public int IntervalCount { get; init; }

    /// <summary>One of <c>licensed</c>, <c>metered</c>.</summary>
    [JsonPropertyName("usageType")]
    public string UsageType { get; init; } = string.Empty;

    /// <summary>One of <c>per_unit</c>, <c>tiered</c>.</summary>
    [JsonPropertyName("billingScheme")]
    public string BillingScheme { get; init; } = string.Empty;

    /// <summary>Free-trial days granted at subscribe time.</summary>
    [JsonPropertyName("trialPeriodDays")]
    public int TrialPeriodDays { get; init; }

    /// <summary>Whether new subscriptions can still be created against this price.</summary>
    [JsonPropertyName("active")]
    public bool Active { get; init; }

    /// <summary>Free-form annotations (arbitrary JSON values).</summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, JsonElement>? Metadata { get; init; }

    /// <summary>The environment (<c>"sandbox"</c> or <c>"live"</c>).</summary>
    [JsonPropertyName("mode")]
    public string Mode { get; init; } = string.Empty;

    /// <summary>When the price was created.</summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>Parameters for creating a price (under a plan, via <c>nombaone.Plans.Prices</c>).</summary>
public sealed class PriceCreateParams
{
    /// <summary>
    /// Amount per unit per interval, in integer kobo. <c>250_000</c> is ₦2,500.00 —
    /// not ₦250,000. Multiply naira by 100 exactly once.
    /// </summary>
    [JsonPropertyName("unitAmountInKobo")]
    public required long UnitAmountInKobo { get; init; }

    /// <summary>One of <c>day</c>, <c>week</c>, <c>month</c>, <c>year</c>.</summary>
    [JsonPropertyName("interval")]
    public required string Interval { get; init; }

    /// <summary>Bill every N intervals. Defaults to <c>1</c> server-side.</summary>
    [JsonPropertyName("intervalCount")]
    public int? IntervalCount { get; init; }

    /// <summary>Defaults to <c>licensed</c> server-side.</summary>
    [JsonPropertyName("usageType")]
    public string? UsageType { get; init; }

    /// <summary>Defaults to <c>per_unit</c> server-side (tiered is not yet chargeable).</summary>
    [JsonPropertyName("billingScheme")]
    public string? BillingScheme { get; init; }

    /// <summary>Free-trial days granted at subscribe time. Defaults to <c>0</c> server-side.</summary>
    [JsonPropertyName("trialPeriodDays")]
    public int? TrialPeriodDays { get; init; }

    /// <summary>Free-form annotations (arbitrary JSON values).</summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }
}

/// <summary>Filters for <see cref="PricesResource.ListAsync"/>.</summary>
public sealed class PriceListParams
{
    /// <summary>Filter to one plan's prices (<c>nbo…pln</c>). Note the wire name is <c>planRef</c>.</summary>
    public string? PlanRef { get; init; }

    /// <summary>Filter by active flag.</summary>
    public bool? Active { get; init; }

    /// <summary>Page size, 1–100 (the API default is 20).</summary>
    public int? Limit { get; init; }

    /// <summary>Opaque cursor from a previous page's <c>Pagination.NextCursor</c>.</summary>
    public string? Cursor { get; init; }
}

/// <summary>
/// Prices — the amounts and cadences plans are sold at. Create and list them
/// under a plan via <c>nombaone.Plans.Prices</c>; this namespace reads and
/// deactivates them directly.
/// </summary>
public sealed class PricesResource : NombaoneResource
{
    internal PricesResource(Nombaone client)
        : base(client)
    {
    }

    /// <summary>Retrieve a price by id.</summary>
    /// <param name="id">The price id (<c>nbo…prc</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="NotFoundException">404 <c>PRICE_NOT_FOUND</c>.</exception>
    public Task<Price> RetrieveAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        GetAsync<Price>($"/prices/{Seg(id)}", options, cancellationToken);

    /// <summary>List prices across all plans, newest first.</summary>
    /// <param name="parameters">Optional filters and page size.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <example>
    /// <code>
    /// var page = await nombaone.Prices.ListAsync(new PriceListParams { PlanRef = plan.Id, Active = true });
    /// </code>
    /// </example>
    public Task<NombaonePage<Price>> ListAsync(PriceListParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAsync<Price>("/prices", BuildListQuery(parameters), options, cancellationToken);

    /// <summary>List prices as an async stream, fetching pages for you as you iterate.</summary>
    /// <param name="parameters">Optional filters and page size.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Stops iteration and cancels in-flight fetches.</param>
    public IAsyncEnumerable<Price> ListAutoPagingAsync(PriceListParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAutoPagingAsync<Price>("/prices", BuildListQuery(parameters), options, cancellationToken);

    /// <summary>
    /// Deactivate a price so no new subscriptions can be created against it.
    /// Existing subscriptions are unaffected — prices are immutable history.
    /// </summary>
    /// <param name="id">The price id (<c>nbo…prc</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="ConflictException">409 <c>PRICE_ALREADY_INACTIVE</c>.</exception>
    public Task<Price> DeactivateAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<Price>($"/prices/{Seg(id)}/deactivate", EmptyBody, options, cancellationToken);

    internal static IReadOnlyDictionary<string, string?> BuildListQuery(PriceListParams? parameters) => new Dictionary<string, string?>
    {
        ["planRef"] = parameters?.PlanRef,
        ["active"] = parameters?.Active?.ToString(CultureInfo.InvariantCulture).ToLowerInvariant(),
        ["limit"] = parameters?.Limit?.ToString(CultureInfo.InvariantCulture),
        ["cursor"] = parameters?.Cursor,
    };
}
