using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NombaOne;

/// <summary>
/// What you sell — "Pro", "Starter". A plan holds the name and description; its
/// prices (amount + cadence) live underneath it.
/// </summary>
public sealed class Plan : NombaoneEntity
{
    /// <summary>Always <c>"plan"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "plan";

    /// <summary>The plan id (<c>nbo…pln</c>).</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>Unique within your organization (<c>PLAN_NAME_TAKEN</c> on reuse).</summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>The plan description, or <c>null</c>.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>Either <c>"active"</c> or <c>"archived"</c>.</summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    /// <summary>Free-form annotations (arbitrary JSON values).</summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, JsonElement>? Metadata { get; init; }

    /// <summary>The environment (<c>"sandbox"</c> or <c>"live"</c>).</summary>
    [JsonPropertyName("mode")]
    public string Mode { get; init; } = string.Empty;

    /// <summary>When the plan was created.</summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>When the plan was last updated.</summary>
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>Parameters for <see cref="PlansResource.CreateAsync"/>.</summary>
public sealed class PlanCreateParams
{
    /// <summary>The plan name (unique within your organization).</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>An optional description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>Free-form annotations (arbitrary JSON values).</summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }
}

/// <summary>Parameters for <see cref="PlansResource.UpdateAsync"/>. At least one field must be provided.</summary>
public sealed class PlanUpdateParams
{
    /// <summary>A new name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// A new description. Assign a value to set it, or
    /// <c>Optional&lt;string&gt;.Null</c> to clear it; leave unset to keep it.
    /// </summary>
    [JsonPropertyName("description")]
    public Optional<string>? Description { get; init; }

    /// <summary>Free-form annotations (arbitrary JSON values).</summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }
}

/// <summary>Filters for <see cref="PlansResource.ListAsync"/>.</summary>
public sealed class PlanListParams
{
    /// <summary>Filter by status: <c>active</c> or <c>archived</c>.</summary>
    public string? Status { get; init; }

    /// <summary>Page size, 1–100 (the API default is 20).</summary>
    public int? Limit { get; init; }

    /// <summary>Opaque cursor from a previous page's <c>Pagination.NextCursor</c>.</summary>
    public string? Cursor { get; init; }
}

/// <summary>Filters for <see cref="PlanPricesResource.ListAsync"/>.</summary>
public sealed class PlanPriceListParams
{
    /// <summary>Page size, 1–100 (the API default is 20).</summary>
    public int? Limit { get; init; }

    /// <summary>Opaque cursor from a previous page's <c>Pagination.NextCursor</c>.</summary>
    public string? Cursor { get; init; }
}

/// <summary>Prices nested under a plan (create/list); see <c>nombaone.Prices</c> for reads and deactivation.</summary>
public sealed class PlanPricesResource : NombaoneResource
{
    internal PlanPricesResource(Nombaone client)
        : base(client)
    {
    }

    /// <summary>
    /// Create a price under a plan. Prices are immutable once created.
    /// </summary>
    /// <param name="planId">The plan id (<c>nbo…pln</c>).</param>
    /// <param name="parameters">The price to create (amount is integer kobo).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <example>
    /// <code>
    /// var price = await nombaone.Plans.Prices.CreateAsync(plan.Id, new PriceCreateParams
    /// {
    ///     UnitAmountInKobo = 250_000, // ₦2,500.00 per month
    ///     Interval = "month",
    /// });
    /// </code>
    /// </example>
    public Task<Price> CreateAsync(string planId, PriceCreateParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<Price>($"/plans/{Seg(planId)}/prices", parameters, options, cancellationToken);

    /// <summary>List a plan's prices, newest first.</summary>
    /// <param name="planId">The plan id (<c>nbo…pln</c>).</param>
    /// <param name="parameters">Optional page size and cursor.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<NombaonePage<Price>> ListAsync(string planId, PlanPriceListParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAsync<Price>($"/plans/{Seg(planId)}/prices", BuildListQuery(parameters), options, cancellationToken);

    /// <summary>List a plan's prices as an async stream, fetching pages for you as you iterate.</summary>
    /// <param name="planId">The plan id (<c>nbo…pln</c>).</param>
    /// <param name="parameters">Optional page size and cursor.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Stops iteration and cancels in-flight fetches.</param>
    public IAsyncEnumerable<Price> ListAutoPagingAsync(string planId, PlanPriceListParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAutoPagingAsync<Price>($"/plans/{Seg(planId)}/prices", BuildListQuery(parameters), options, cancellationToken);

    private static IReadOnlyDictionary<string, string?> BuildListQuery(PlanPriceListParams? parameters) => new Dictionary<string, string?>
    {
        ["limit"] = parameters?.Limit?.ToString(CultureInfo.InvariantCulture),
        ["cursor"] = parameters?.Cursor,
    };
}

/// <summary>Plans — your catalog. Prices nest under <c>nombaone.Plans.Prices</c>.</summary>
/// <example>
/// <code>
/// var plan = await nombaone.Plans.CreateAsync(new PlanCreateParams { Name = "Pro" });
/// var price = await nombaone.Plans.Prices.CreateAsync(plan.Id, new PriceCreateParams
/// {
///     UnitAmountInKobo = 250_000,
///     Interval = "month",
/// });
/// </code>
/// </example>
public sealed class PlansResource : NombaoneResource
{
    internal PlansResource(Nombaone client)
        : base(client)
    {
        Prices = new PlanPricesResource(client);
    }

    /// <summary>Prices nested under a plan.</summary>
    public PlanPricesResource Prices { get; }

    /// <summary>Create a plan.</summary>
    /// <param name="parameters">The plan to create.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="ConflictException">409 <c>PLAN_NAME_TAKEN</c>.</exception>
    public Task<Plan> CreateAsync(PlanCreateParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<Plan>("/plans", parameters, options, cancellationToken);

    /// <summary>Retrieve a plan by id.</summary>
    /// <param name="id">The plan id (<c>nbo…pln</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="NotFoundException">404 <c>PLAN_NOT_FOUND</c>.</exception>
    public Task<Plan> RetrieveAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        GetAsync<Plan>($"/plans/{Seg(id)}", options, cancellationToken);

    /// <summary>Update a plan's mutable fields. At least one field is required.</summary>
    /// <param name="id">The plan id (<c>nbo…pln</c>).</param>
    /// <param name="parameters">The fields to change.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<Plan> UpdateAsync(string id, PlanUpdateParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PatchAsync<Plan>($"/plans/{Seg(id)}", parameters, options, cancellationToken);

    /// <summary>List plans, newest first.</summary>
    /// <param name="parameters">Optional filters and page size.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<NombaonePage<Plan>> ListAsync(PlanListParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAsync<Plan>("/plans", BuildListQuery(parameters), options, cancellationToken);

    /// <summary>List plans as an async stream, fetching pages for you as you iterate.</summary>
    /// <param name="parameters">Optional filters and page size.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Stops iteration and cancels in-flight fetches.</param>
    public IAsyncEnumerable<Plan> ListAutoPagingAsync(PlanListParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAutoPagingAsync<Plan>("/plans", BuildListQuery(parameters), options, cancellationToken);

    /// <summary>
    /// Archive a plan — it stops being subscribable, but its history stays.
    /// </summary>
    /// <param name="id">The plan id (<c>nbo…pln</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="ConflictException">409 <c>PLAN_ALREADY_ARCHIVED</c>.</exception>
    /// <exception cref="ConflictException">409 <c>PLAN_HAS_ACTIVE_SUBSCRIBERS</c> — migrate or cancel those subscriptions first.</exception>
    public Task<Plan> ArchiveAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<Plan>($"/plans/{Seg(id)}/archive", EmptyBody, options, cancellationToken);

    private static IReadOnlyDictionary<string, string?> BuildListQuery(PlanListParams? parameters) => new Dictionary<string, string?>
    {
        ["status"] = parameters?.Status,
        ["limit"] = parameters?.Limit?.ToString(CultureInfo.InvariantCulture),
        ["cursor"] = parameters?.Cursor,
    };
}
