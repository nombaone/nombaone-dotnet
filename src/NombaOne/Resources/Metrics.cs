using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NombaOne;

/// <summary>Recovery funnel counts inside a metrics window.</summary>
public sealed class DunningFunnel
{
    /// <summary>Attempts scheduled.</summary>
    [JsonPropertyName("scheduled")]
    public int Scheduled { get; init; }

    /// <summary>Attempts in progress.</summary>
    [JsonPropertyName("attempting")]
    public int Attempting { get; init; }

    /// <summary>Attempts blocked pending a card update.</summary>
    [JsonPropertyName("cardUpdateRequired")]
    public int CardUpdateRequired { get; init; }

    /// <summary>Attempts rescheduled.</summary>
    [JsonPropertyName("rescheduled")]
    public int Rescheduled { get; init; }

    /// <summary>Attempts that succeeded.</summary>
    [JsonPropertyName("succeeded")]
    public int Succeeded { get; init; }

    /// <summary>Attempts exhausted.</summary>
    [JsonPropertyName("exhausted")]
    public int Exhausted { get; init; }
}

/// <summary>Billing KPIs, computed from the ledger on read — never stored, never stale.</summary>
public sealed class BillingMetrics : NombaoneEntity
{
    /// <summary>Always <c>"billing_metrics"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "billing_metrics";

    /// <summary>Monthly recurring revenue, in integer kobo.</summary>
    [JsonPropertyName("mrrInKobo")]
    public long MrrInKobo { get; init; }

    /// <summary>The number of active subscriptions.</summary>
    [JsonPropertyName("activeCount")]
    public int ActiveCount { get; init; }

    /// <summary>Voluntary churn count in the window.</summary>
    [JsonPropertyName("voluntaryChurn")]
    public int VoluntaryChurn { get; init; }

    /// <summary>Involuntary churn count in the window.</summary>
    [JsonPropertyName("involuntaryChurn")]
    public int InvoluntaryChurn { get; init; }

    /// <summary>The failed-charge rate (0–1).</summary>
    [JsonPropertyName("failedChargeRate")]
    public double FailedChargeRate { get; init; }

    /// <summary>The dunning recovery rate (0–1).</summary>
    [JsonPropertyName("dunningRecoveryRate")]
    public double DunningRecoveryRate { get; init; }

    /// <summary>The recovery funnel counts.</summary>
    [JsonPropertyName("dunningFunnel")]
    public DunningFunnel DunningFunnel { get; init; } = new();

    /// <summary>The window start.</summary>
    [JsonPropertyName("windowFrom")]
    public DateTimeOffset WindowFrom { get; init; }

    /// <summary>The window end.</summary>
    [JsonPropertyName("windowTo")]
    public DateTimeOffset WindowTo { get; init; }
}

/// <summary>Parameters for <see cref="MetricsResource.BillingAsync"/>.</summary>
public sealed class BillingMetricsParams
{
    /// <summary>ISO-8601 date-time, start of the window.</summary>
    public string? From { get; init; }

    /// <summary>ISO-8601 date-time, end of the window.</summary>
    public string? To { get; init; }
}

/// <summary>Metrics — MRR, churn, and the dunning funnel.</summary>
public sealed class MetricsResource : NombaoneResource
{
    internal MetricsResource(Nombaone client)
        : base(client)
    {
    }

    /// <summary>Billing KPIs over a window (defaults to a recent window server-side).</summary>
    /// <param name="parameters">Optional window bounds.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <example>
    /// <code>
    /// var metrics = await nombaone.Metrics.BillingAsync();
    /// Console.WriteLine($"MRR ₦{metrics.MrrInKobo / 100m}");
    /// </code>
    /// </example>
    public Task<BillingMetrics> BillingAsync(BillingMetricsParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        GetAsync<BillingMetrics>("/metrics/billing", new Dictionary<string, string?>
        {
            ["from"] = parameters?.From,
            ["to"] = parameters?.To,
        }, options, cancellationToken);
}
