using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NombaOne;

/// <summary>
/// A coupon applied to a customer or subscription — the <i>application</i>, not
/// the coupon definition.
/// </summary>
public sealed class Discount : NombaoneEntity
{
    /// <summary>Always <c>"discount"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "discount";

    /// <summary>The discount id (<c>nbo…dsc</c>).</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>The coupon this discount was created from (<c>nbo…cpn</c>).</summary>
    [JsonPropertyName("couponId")]
    public string CouponId { get; init; } = string.Empty;

    /// <summary>The customer this discount applies to, or <c>null</c> if it is subscription-scoped.</summary>
    [JsonPropertyName("customerId")]
    public string? CustomerId { get; init; }

    /// <summary>The subscription this discount applies to, or <c>null</c> if it is customer-scoped.</summary>
    [JsonPropertyName("subscriptionId")]
    public string? SubscriptionId { get; init; }

    /// <summary>Either <c>"active"</c> or <c>"ended"</c>.</summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    /// <summary>Cycles left for a <c>repeating</c> coupon; <c>null</c> for <c>once</c>/<c>forever</c>.</summary>
    [JsonPropertyName("cyclesRemaining")]
    public int? CyclesRemaining { get; init; }

    /// <summary>When the discount began applying.</summary>
    [JsonPropertyName("startAt")]
    public DateTimeOffset StartAt { get; init; }

    /// <summary>When the discount ends, or <c>null</c> if open-ended.</summary>
    [JsonPropertyName("endAt")]
    public DateTimeOffset? EndAt { get; init; }

    /// <summary>The environment (<c>"sandbox"</c> or <c>"live"</c>).</summary>
    [JsonPropertyName("mode")]
    public string Mode { get; init; } = string.Empty;

    /// <summary>When the discount was created.</summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// An entry in the append-only domain-event log — the audit trail behind every
/// webhook. <see cref="Payload"/> carries the same <c>data</c> your endpoints
/// receive.
/// </summary>
public sealed class DomainEvent : NombaoneEntity
{
    /// <summary>Always <c>"event"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "event";

    /// <summary>The event id (<c>nbo…evt</c>) — the id webhook receivers dedupe on.</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>The catalog event type, e.g. <c>invoice.paid</c>.</summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    /// <summary>The event payload (arbitrary JSON).</summary>
    [JsonPropertyName("payload")]
    public IReadOnlyDictionary<string, JsonElement>? Payload { get; init; }

    /// <summary>When the event occurred.</summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>One line on an invoice. Amounts are integer kobo; discount and credit lines are negative.</summary>
public sealed class InvoiceLineItem
{
    /// <summary>The line-item id.</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>One of <c>subscription</c>, <c>proration</c>, <c>discount</c>, <c>credit</c>, <c>adjustment</c>.</summary>
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = string.Empty;

    /// <summary>A human-readable description of the line.</summary>
    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    /// <summary>The line amount in integer kobo (₦1.00 = 100). Negative for discount/credit lines.</summary>
    [JsonPropertyName("amountInKobo")]
    public long AmountInKobo { get; init; }

    /// <summary>The line quantity.</summary>
    [JsonPropertyName("quantity")]
    public int Quantity { get; init; }
}
