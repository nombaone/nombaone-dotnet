using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using NombaOne.Internal;

namespace NombaOne.Webhooks;

/// <summary>The underlying domain event carried by a delivery. Dedupe on <see cref="Id"/>.</summary>
public sealed class WebhookEventRef
{
    /// <summary>The domain-event id (<c>nbo…evt</c>) — the id to dedupe on. Replays keep it stable.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The event type, e.g. <c>invoice.paid</c>.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>When the underlying event occurred, if present.</summary>
    public DateTimeOffset? CreatedAt { get; init; }
}

/// <summary>
/// The <c>data</c> payload of a delivery. Every event carries a
/// <see cref="Reference"/>; the remaining properties are populated only for the
/// event types that include them. Anything not modeled here is available on
/// <see cref="Raw"/>.
/// </summary>
public sealed class WebhookEventData
{
    /// <summary>The affected resource's public id (<c>nbo…</c>).</summary>
    [JsonPropertyName("reference")]
    public string? Reference { get; init; }

    /// <summary>The failure/expiry reason (<c>invoice.payment_failed</c>, <c>invoice.action_required</c>, <c>payment_method.expiring</c>).</summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    /// <summary>Amount collected so far, in integer kobo (<c>invoice.payment_partially_collected</c>).</summary>
    [JsonPropertyName("amountPaid")]
    public long? AmountPaid { get; init; }

    /// <summary>Amount still outstanding, in integer kobo (<c>invoice.payment_partially_collected</c>).</summary>
    [JsonPropertyName("amountRemaining")]
    public long? AmountRemaining { get; init; }

    /// <summary>Where to send the customer to authenticate (<c>invoice.action_required</c>).</summary>
    [JsonPropertyName("checkoutLink")]
    public string? CheckoutLink { get; init; }

    /// <summary>The payment-method kind (<c>payment_method.attached</c>).</summary>
    [JsonPropertyName("kind")]
    public string? Kind { get; init; }

    /// <summary>A status (<c>payment_method.attached</c>, <c>subscription.created</c>).</summary>
    [JsonPropertyName("status")]
    public string? Status { get; init; }

    /// <summary>The affected subscription (<c>payment_method.updated</c>).</summary>
    [JsonPropertyName("subscription")]
    public string? Subscription { get; init; }

    /// <summary>The coupon code (<c>coupon.created</c>).</summary>
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    /// <summary>The full, raw <c>data</c> object — read fields not modeled above from here.</summary>
    [JsonIgnore]
    public JsonElement Raw { get; internal set; }
}

/// <summary>
/// A verified, parsed webhook delivery. Narrow on <see cref="Type"/> (compare
/// against <see cref="WebhookEventTypes"/>) and read the matching typed fields
/// off <see cref="Data"/>. The catalog is open — an event type added by the API
/// tomorrow still parses today.
/// </summary>
/// <example>
/// <code>
/// var evt = WebhookVerifier.ConstructEvent(rawBody, signatureHeader, secret);
/// if (alreadyProcessed(evt.Event.Id)) return; // at-least-once ⇒ dedupe on Event.Id
/// switch (evt.Type)
/// {
///     case WebhookEventTypes.InvoicePaid:            Unlock(evt.Data.Reference!);   break;
///     case WebhookEventTypes.InvoiceActionRequired:  Email(evt.Data.CheckoutLink!); break;
///     case WebhookEventTypes.InvoicePaymentFailed:   Note(evt.Data.Reason!);        break;
/// }
/// </code>
/// </example>
public sealed class WebhookEvent
{
    internal WebhookEvent(string id, string type, WebhookEventRef @event, WebhookEventData data)
    {
        Id = id;
        Type = type;
        Event = @event;
        Data = data;
    }

    /// <summary>The delivery reference (<c>nbo…whd</c>) — unique per delivery attempt-target.</summary>
    public string Id { get; }

    /// <summary>The event type, e.g. <c>invoice.paid</c>.</summary>
    public string Type { get; }

    /// <summary>The underlying domain event. <b>Dedupe on <c>Event.Id</c></b> — delivery is at-least-once.</summary>
    public WebhookEventRef Event { get; }

    /// <summary>The event payload.</summary>
    public WebhookEventData Data { get; }

    internal static WebhookEvent Parse(string rawBody)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(rawBody);
        }
        catch (JsonException)
        {
            throw new WebhookVerificationException("Webhook payload was not valid JSON.");
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new WebhookVerificationException("Webhook payload was not a JSON object.");
            }

            var id = GetString(root, "id") ?? string.Empty;
            var type = GetString(root, "type") ?? string.Empty;

            WebhookEventRef eventRef;
            if (root.TryGetProperty("event", out var eventElement) && eventElement.ValueKind == JsonValueKind.Object)
            {
                eventRef = new WebhookEventRef
                {
                    Id = GetString(eventElement, "id") ?? string.Empty,
                    Type = GetString(eventElement, "type") ?? string.Empty,
                    CreatedAt = GetDate(eventElement, "createdAt"),
                };
            }
            else
            {
                // Defensive: guarantee a dedupe-able Event.Id even if a delivery body
                // arrives flat (older shape) — fall back to the top-level fields.
                eventRef = new WebhookEventRef { Id = id, Type = type, CreatedAt = GetDate(root, "createdAt") };
            }

            WebhookEventData data;
            if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Object)
            {
                data = JsonSerializer.Deserialize<WebhookEventData>(dataElement.GetRawText(), NombaoneJson.Options)
                       ?? new WebhookEventData();
                data.Raw = dataElement.Clone();
            }
            else
            {
                data = new WebhookEventData();
            }

            return new WebhookEvent(id, type, eventRef, data);
        }
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static DateTimeOffset? GetDate(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.String &&
        value.TryGetDateTimeOffset(out var parsed)
            ? parsed
            : null;
}
