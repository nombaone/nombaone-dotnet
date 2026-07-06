using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NombaOne;

/// <summary>The platform fee configuration.</summary>
public sealed class PlatformFee
{
    /// <summary>Fee in basis points, or <c>null</c>.</summary>
    [JsonPropertyName("bps")]
    public int? Bps { get; init; }

    /// <summary>Minimum fee in integer kobo, or <c>null</c>.</summary>
    [JsonPropertyName("minInKobo")]
    public long? MinInKobo { get; init; }

    /// <summary>Maximum fee in integer kobo, or <c>null</c>.</summary>
    [JsonPropertyName("maxInKobo")]
    public long? MaxInKobo { get; init; }
}

/// <summary>The org's grace/dunning summary.</summary>
public sealed class GraceSettings
{
    /// <summary>The grace-period length in hours.</summary>
    [JsonPropertyName("gracePeriodHours")]
    public int GracePeriodHours { get; init; }

    /// <summary>The maximum number of dunning attempts.</summary>
    [JsonPropertyName("dunningMaxAttempts")]
    public int DunningMaxAttempts { get; init; }
}

/// <summary>Org branding shown on hosted pages and comms.</summary>
public sealed class Branding
{
    /// <summary>The display name.</summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    /// <summary>The support email.</summary>
    [JsonPropertyName("supportEmail")]
    public string? SupportEmail { get; init; }

    /// <summary>The logo URL.</summary>
    [JsonPropertyName("logoUrl")]
    public string? LogoUrl { get; init; }

    /// <summary>The primary brand color as a hex string (e.g. <c>#1A2B3C</c>).</summary>
    [JsonPropertyName("primaryColorHex")]
    public string? PrimaryColorHex { get; init; }
}

/// <summary>The billing block of <see cref="TenantSettings"/>.</summary>
public sealed class TenantBilling
{
    /// <summary>The per-minute request cap, or <c>null</c>.</summary>
    [JsonPropertyName("rateLimitPerMinute")]
    public int? RateLimitPerMinute { get; init; }

    /// <summary>The monthly request quota, or <c>null</c>.</summary>
    [JsonPropertyName("monthlyRequestQuota")]
    public int? MonthlyRequestQuota { get; init; }

    /// <summary>One of <c>split_at_collection</c>, <c>collect_then_payout</c>.</summary>
    [JsonPropertyName("settlementMode")]
    public string SettlementMode { get; init; } = string.Empty;

    /// <summary>The platform fee configuration.</summary>
    [JsonPropertyName("platformFee")]
    public PlatformFee PlatformFee { get; init; } = new();

    /// <summary>The grace/dunning summary.</summary>
    [JsonPropertyName("grace")]
    public GraceSettings Grace { get; init; } = new();

    /// <summary>The org branding.</summary>
    [JsonPropertyName("branding")]
    public Branding Branding { get; init; } = new();
}

/// <summary>The webhook block of <see cref="TenantSettings"/>.</summary>
public sealed class TenantWebhook
{
    /// <summary>The configured webhook URL, or <c>null</c>.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    /// <summary>The signing-secret prefix, or <c>null</c>.</summary>
    [JsonPropertyName("signingSecretPrefix")]
    public string? SigningSecretPrefix { get; init; }

    /// <summary>Whether a webhook is configured.</summary>
    [JsonPropertyName("configured")]
    public bool Configured { get; init; }
}

/// <summary>The Nomba provider account block of <see cref="TenantSettings"/>.</summary>
public sealed class NombaAccount
{
    /// <summary>The provider account reference, or <c>null</c>.</summary>
    [JsonPropertyName("accountRef")]
    public string? AccountRef { get; init; }

    /// <summary>The provider account status, or <c>null</c>.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; init; }
}

/// <summary>Org-level configuration: limits, settlement mode, branding, webhook + Nomba account status.</summary>
public sealed class TenantSettings : NombaoneEntity
{
    /// <summary>Always <c>"organization"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "organization";

    /// <summary>Billing limits, settlement mode, fees, grace, and branding.</summary>
    [JsonPropertyName("billing")]
    public TenantBilling Billing { get; init; } = new();

    /// <summary>The org's outbound webhook configuration summary.</summary>
    [JsonPropertyName("webhook")]
    public TenantWebhook Webhook { get; init; } = new();

    /// <summary>The Nomba provider account summary.</summary>
    [JsonPropertyName("nombaAccount")]
    public NombaAccount NombaAccount { get; init; } = new();
}

/// <summary>
/// Your org-wide billing + dunning policy — how hard and when the engine
/// retries, payday bias, grace windows, and collection defaults.
/// </summary>
public sealed class BillingSettings : NombaoneEntity
{
    /// <summary>Always <c>"billing_settings"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "billing_settings";

    /// <summary>Whether partial collection is enabled.</summary>
    [JsonPropertyName("partialCollectionEnabled")]
    public bool PartialCollectionEnabled { get; init; }

    /// <summary>One of <c>credit_next_cycle</c>, <c>none</c>.</summary>
    [JsonPropertyName("prorationCreditPolicy")]
    public string ProrationCreditPolicy { get; init; } = string.Empty;

    /// <summary>The maximum number of dunning attempts.</summary>
    [JsonPropertyName("dunningMaxAttempts")]
    public int DunningMaxAttempts { get; init; }

    /// <summary>The dunning retry intervals, in hours.</summary>
    [JsonPropertyName("dunningIntervalsHours")]
    public IReadOnlyList<int> DunningIntervalsHours { get; init; } = System.Array.Empty<int>();

    /// <summary>The maximum dunning window, in hours.</summary>
    [JsonPropertyName("dunningMaxWindowHours")]
    public int DunningMaxWindowHours { get; init; }

    /// <summary>The grace-period length, in hours.</summary>
    [JsonPropertyName("gracePeriodHours")]
    public int GracePeriodHours { get; init; }

    /// <summary>Days of month treated as paydays (retries bias toward them).</summary>
    [JsonPropertyName("paydayDays")]
    public IReadOnlyList<int> PaydayDays { get; init; } = System.Array.Empty<int>();

    /// <summary>How many days early a retry may pull forward toward a payday.</summary>
    [JsonPropertyName("paydayPullForwardDays")]
    public int PaydayPullForwardDays { get; init; }

    /// <summary>Whether payday bias is enabled.</summary>
    [JsonPropertyName("paydayBiasEnabled")]
    public bool PaydayBiasEnabled { get; init; }

    /// <summary>One of <c>charge_automatically</c>, <c>send_invoice</c>.</summary>
    [JsonPropertyName("defaultCollectionMethod")]
    public string DefaultCollectionMethod { get; init; } = string.Empty;

    /// <summary>Whether customer comms are enabled.</summary>
    [JsonPropertyName("commsEnabled")]
    public bool CommsEnabled { get; init; }
}

/// <summary>Branding fields for <see cref="TenantSettingsUpdateParams"/>.</summary>
public sealed class BrandingParams
{
    /// <summary>The display name.</summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    /// <summary>The support email.</summary>
    [JsonPropertyName("supportEmail")]
    public string? SupportEmail { get; init; }

    /// <summary>The logo URL.</summary>
    [JsonPropertyName("logoUrl")]
    public string? LogoUrl { get; init; }

    /// <summary>The primary brand color as a hex string (e.g. <c>#1A2B3C</c>).</summary>
    [JsonPropertyName("primaryColorHex")]
    public string? PrimaryColorHex { get; init; }
}

/// <summary>
/// Parameters for <see cref="OrganizationResource.UpdateAsync"/>. At least one
/// field must be provided. Rate limits are operator-set (not here).
/// </summary>
public sealed class TenantSettingsUpdateParams
{
    /// <summary>A new monthly request quota.</summary>
    [JsonPropertyName("monthlyRequestQuota")]
    public int? MonthlyRequestQuota { get; init; }

    /// <summary>A new settlement mode: <c>split_at_collection</c> or <c>collect_then_payout</c>.</summary>
    [JsonPropertyName("settlementMode")]
    public string? SettlementMode { get; init; }

    /// <summary>New branding.</summary>
    [JsonPropertyName("branding")]
    public BrandingParams? Branding { get; init; }
}

/// <summary>
/// Parameters for <see cref="OrganizationBillingResource.UpdateAsync"/>. PUT
/// semantics — only the supplied keys change; every field is optional.
/// </summary>
public sealed class BillingSettingsUpdateParams
{
    /// <summary>Enable or disable partial collection.</summary>
    [JsonPropertyName("partialCollectionEnabled")]
    public bool? PartialCollectionEnabled { get; init; }

    /// <summary>One of <c>credit_next_cycle</c>, <c>none</c>.</summary>
    [JsonPropertyName("prorationCreditPolicy")]
    public string? ProrationCreditPolicy { get; init; }

    /// <summary>The maximum number of dunning attempts (1–10).</summary>
    [JsonPropertyName("dunningMaxAttempts")]
    public int? DunningMaxAttempts { get; init; }

    /// <summary>The dunning retry intervals, in hours.</summary>
    [JsonPropertyName("dunningIntervalsHours")]
    public IReadOnlyList<int>? DunningIntervalsHours { get; init; }

    /// <summary>The maximum dunning window, in hours (must be ≥ the largest configured interval).</summary>
    [JsonPropertyName("dunningMaxWindowHours")]
    public int? DunningMaxWindowHours { get; init; }

    /// <summary>The grace-period length, in hours.</summary>
    [JsonPropertyName("gracePeriodHours")]
    public int? GracePeriodHours { get; init; }

    /// <summary>Days of month (1–31) treated as paydays.</summary>
    [JsonPropertyName("paydayDays")]
    public IReadOnlyList<int>? PaydayDays { get; init; }

    /// <summary>How many days early a retry may pull forward (0–28).</summary>
    [JsonPropertyName("paydayPullForwardDays")]
    public int? PaydayPullForwardDays { get; init; }

    /// <summary>Enable or disable payday bias.</summary>
    [JsonPropertyName("paydayBiasEnabled")]
    public bool? PaydayBiasEnabled { get; init; }

    /// <summary>One of <c>charge_automatically</c>, <c>send_invoice</c>.</summary>
    [JsonPropertyName("defaultCollectionMethod")]
    public string? DefaultCollectionMethod { get; init; }

    /// <summary>Enable or disable customer comms.</summary>
    [JsonPropertyName("commsEnabled")]
    public bool? CommsEnabled { get; init; }
}

/// <summary>Billing + dunning policy under <c>/organization/billing</c>.</summary>
public sealed class OrganizationBillingResource : NombaoneResource
{
    internal OrganizationBillingResource(Nombaone client)
        : base(client)
    {
    }

    /// <summary>Read the org's billing + dunning policy.</summary>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<BillingSettings> RetrieveAsync(RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        GetAsync<BillingSettings>("/organization/billing", options, cancellationToken);

    /// <summary>Update the billing policy. PUT semantics, but only supplied keys change.</summary>
    /// <param name="parameters">The fields to change.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <example>
    /// <code>
    /// await nombaone.Organization.Billing.UpdateAsync(new BillingSettingsUpdateParams
    /// {
    ///     PaydayBiasEnabled = true,
    ///     PaydayDays = new[] { 25, 28, 30 },
    /// });
    /// </code>
    /// </example>
    public Task<BillingSettings> UpdateAsync(BillingSettingsUpdateParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PutAsync<BillingSettings>("/organization/billing", parameters, options, cancellationToken);
}

/// <summary>Organization settings — configuration, not a billing object.</summary>
public sealed class OrganizationResource : NombaoneResource
{
    internal OrganizationResource(Nombaone client)
        : base(client)
    {
        Billing = new OrganizationBillingResource(client);
    }

    /// <summary>Billing + dunning policy.</summary>
    public OrganizationBillingResource Billing { get; }

    /// <summary>Read org-level settings (limits, settlement mode, branding, statuses).</summary>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<TenantSettings> RetrieveAsync(RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        GetAsync<TenantSettings>("/organization", options, cancellationToken);

    /// <summary>Update tenant-editable settings. At least one field is required.</summary>
    /// <param name="parameters">The fields to change.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<TenantSettings> UpdateAsync(TenantSettingsUpdateParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PutAsync<TenantSettings>("/organization", parameters, options, cancellationToken);
}
