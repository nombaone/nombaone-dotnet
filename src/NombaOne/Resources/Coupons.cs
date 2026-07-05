using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NombaOne;

/// <summary>
/// A reusable discount rule. Applying a coupon to a customer or subscription
/// creates a <see cref="Discount"/> — the coupon is the rule, the discount is
/// one application of it.
/// </summary>
public sealed class Coupon : NombaoneEntity
{
    /// <summary>Always <c>"coupon"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "coupon";

    /// <summary>The coupon id (<c>nbo…cpn</c>).</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>The tenant-facing redemption code, e.g. <c>LAUNCH20</c>.</summary>
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    /// <summary>One of <c>once</c>, <c>repeating</c>, <c>forever</c>.</summary>
    [JsonPropertyName("duration")]
    public string Duration { get; init; } = string.Empty;

    /// <summary>Fixed discount in integer kobo, or <c>null</c> if this is a percentage coupon.</summary>
    [JsonPropertyName("amountOffInKobo")]
    public long? AmountOffInKobo { get; init; }

    /// <summary>Percentage discount (1–100), or <c>null</c> if this is a fixed-amount coupon.</summary>
    [JsonPropertyName("percentOff")]
    public int? PercentOff { get; init; }

    /// <summary>Cycles a <c>repeating</c> coupon lasts, or <c>null</c>.</summary>
    [JsonPropertyName("durationInCycles")]
    public int? DurationInCycles { get; init; }

    /// <summary>The date after which the coupon can no longer be applied, or <c>null</c>.</summary>
    [JsonPropertyName("redeemBy")]
    public DateTimeOffset? RedeemBy { get; init; }

    /// <summary>The maximum number of redemptions, or <c>null</c> for unlimited.</summary>
    [JsonPropertyName("maxRedemptions")]
    public int? MaxRedemptions { get; init; }

    /// <summary>How many times the coupon has been redeemed.</summary>
    [JsonPropertyName("timesRedeemed")]
    public int TimesRedeemed { get; init; }

    /// <summary>The environment (<c>"sandbox"</c> or <c>"live"</c>).</summary>
    [JsonPropertyName("mode")]
    public string Mode { get; init; } = string.Empty;

    /// <summary>When the coupon was created.</summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Parameters for <see cref="CouponsResource.CreateAsync"/>. Exactly one of
/// <see cref="AmountOffInKobo"/> or <see cref="PercentOff"/> must be set.
/// </summary>
public sealed class CouponCreateParams
{
    /// <summary>The redemption code, e.g. <c>LAUNCH20</c>.</summary>
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    /// <summary>One of <c>once</c>, <c>repeating</c>, <c>forever</c>.</summary>
    [JsonPropertyName("duration")]
    public required string Duration { get; init; }

    /// <summary>Fixed discount, in integer kobo (₦1.00 = 100). Set this <b>or</b> <see cref="PercentOff"/>, not both.</summary>
    [JsonPropertyName("amountOffInKobo")]
    public long? AmountOffInKobo { get; init; }

    /// <summary>Percentage discount, 1–100. Set this <b>or</b> <see cref="AmountOffInKobo"/>, not both.</summary>
    [JsonPropertyName("percentOff")]
    public int? PercentOff { get; init; }

    /// <summary>Required when <see cref="Duration"/> is <c>repeating</c>.</summary>
    [JsonPropertyName("durationInCycles")]
    public int? DurationInCycles { get; init; }

    /// <summary>ISO-8601 date-time after which the coupon can no longer be applied.</summary>
    [JsonPropertyName("redeemBy")]
    public string? RedeemBy { get; init; }

    /// <summary>The maximum number of redemptions.</summary>
    [JsonPropertyName("maxRedemptions")]
    public int? MaxRedemptions { get; init; }

    /// <summary>Free-form annotations (arbitrary JSON values).</summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }
}

/// <summary>Parameters for <see cref="CouponsResource.UpdateAsync"/>. At least one field must be provided.</summary>
public sealed class CouponUpdateParams
{
    /// <summary>A new redeem-by date (ISO-8601).</summary>
    [JsonPropertyName("redeemBy")]
    public string? RedeemBy { get; init; }

    /// <summary>A new maximum number of redemptions.</summary>
    [JsonPropertyName("maxRedemptions")]
    public int? MaxRedemptions { get; init; }

    /// <summary>Free-form annotations (arbitrary JSON values).</summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }
}

/// <summary>Filters for <see cref="CouponsResource.ListAsync"/>.</summary>
public sealed class CouponListParams
{
    /// <summary>Page size, 1–100 (the API default is 20).</summary>
    public int? Limit { get; init; }

    /// <summary>Opaque cursor from a previous page's <c>Pagination.NextCursor</c>.</summary>
    public string? Cursor { get; init; }
}

/// <summary>
/// Coupons — discount rules you apply via <c>Customers.ApplyDiscountAsync</c> /
/// <c>Subscriptions.ApplyDiscountAsync</c>.
/// </summary>
/// <example>
/// <code>
/// var coupon = await nombaone.Coupons.CreateAsync(new CouponCreateParams
/// {
///     Code = "LAUNCH20",
///     PercentOff = 20,
///     Duration = "repeating",
///     DurationInCycles = 3,
/// });
/// </code>
/// </example>
public sealed class CouponsResource : NombaoneResource
{
    internal CouponsResource(Nombaone client)
        : base(client)
    {
    }

    /// <summary>Create a coupon.</summary>
    /// <param name="parameters">The coupon to create.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="ValidationException">422 <c>COUPON_INVALID_DEFINITION</c> — set exactly one of <c>AmountOffInKobo</c> / <c>PercentOff</c>.</exception>
    public Task<Coupon> CreateAsync(CouponCreateParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<Coupon>("/coupons", parameters, options, cancellationToken);

    /// <summary>Retrieve a coupon by id.</summary>
    /// <param name="id">The coupon id (<c>nbo…cpn</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="NotFoundException">404 <c>COUPON_NOT_FOUND</c>.</exception>
    public Task<Coupon> RetrieveAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        GetAsync<Coupon>($"/coupons/{Seg(id)}", options, cancellationToken);

    /// <summary>Update a coupon's redeem-by, max redemptions, or metadata.</summary>
    /// <param name="id">The coupon id (<c>nbo…cpn</c>).</param>
    /// <param name="parameters">The fields to change.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<Coupon> UpdateAsync(string id, CouponUpdateParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PatchAsync<Coupon>($"/coupons/{Seg(id)}", parameters, options, cancellationToken);

    /// <summary>List coupons, newest first.</summary>
    /// <param name="parameters">Optional page size and cursor.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<NombaonePage<Coupon>> ListAsync(CouponListParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAsync<Coupon>("/coupons", BuildListQuery(parameters), options, cancellationToken);

    /// <summary>List coupons as an async stream, fetching pages for you as you iterate.</summary>
    /// <param name="parameters">Optional page size and cursor.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Stops iteration and cancels in-flight fetches.</param>
    public IAsyncEnumerable<Coupon> ListAutoPagingAsync(CouponListParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAutoPagingAsync<Coupon>("/coupons", BuildListQuery(parameters), options, cancellationToken);

    private static IReadOnlyDictionary<string, string?> BuildListQuery(CouponListParams? parameters) => new Dictionary<string, string?>
    {
        ["limit"] = parameters?.Limit?.ToString(CultureInfo.InvariantCulture),
        ["cursor"] = parameters?.Cursor,
    };
}
