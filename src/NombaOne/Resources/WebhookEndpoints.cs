using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NombaOne;

/// <summary>A URL you registered to receive signed event deliveries.</summary>
public class WebhookEndpoint : NombaoneEntity
{
    /// <summary>Always <c>"webhook"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "webhook";

    /// <summary>The endpoint id (<c>nbo…whk</c>).</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>The endpoint URL.</summary>
    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    /// <summary>Event types fanned out to this endpoint; <c>["*"]</c> means everything.</summary>
    [JsonPropertyName("enabledEvents")]
    public IReadOnlyList<string> EnabledEvents { get; init; } = Array.Empty<string>();

    /// <summary>Display prefix of the signing secret (the full secret is shown once).</summary>
    [JsonPropertyName("signingSecretPrefix")]
    public string SigningSecretPrefix { get; init; } = string.Empty;

    /// <summary>When the endpoint was disabled, or <c>null</c> if enabled.</summary>
    [JsonPropertyName("disabledAt")]
    public DateTimeOffset? DisabledAt { get; init; }

    /// <summary>When the endpoint was created.</summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Returned by <see cref="WebhookEndpointsResource.CreateAsync"/> only — the one
/// time the full signing secret is visible.
/// </summary>
public sealed class WebhookEndpointWithSecret : WebhookEndpoint
{
    /// <summary>
    /// The full signing secret. <b>Shown exactly once</b> — store it now; it is
    /// not recoverable later (only rotatable).
    /// </summary>
    [JsonPropertyName("signingSecret")]
    public string SigningSecret { get; init; } = string.Empty;
}

/// <summary>Returned by <see cref="WebhookEndpointsResource.RotateSecretAsync"/> — again, the only time this secret is visible.</summary>
public sealed class RotatedWebhookSecret : NombaoneEntity
{
    /// <summary>Always <c>"webhook_secret"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "webhook_secret";

    /// <summary>The endpoint id (<c>nbo…whk</c>).</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>The new full signing secret — shown exactly once.</summary>
    [JsonPropertyName("signingSecret")]
    public string SigningSecret { get; init; } = string.Empty;

    /// <summary>Display prefix of the new signing secret.</summary>
    [JsonPropertyName("signingSecretPrefix")]
    public string SigningSecretPrefix { get; init; } = string.Empty;
}

/// <summary>One attempt-tracked delivery of an event to one endpoint.</summary>
public sealed class WebhookDelivery : NombaoneEntity
{
    /// <summary>Always <c>"webhook_delivery"</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = "webhook_delivery";

    /// <summary>The delivery id (<c>nbo…whd</c>).</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>The event type delivered.</summary>
    [JsonPropertyName("eventType")]
    public string EventType { get; init; } = string.Empty;

    /// <summary>The endpoint this delivery targets (<c>nbo…whk</c>).</summary>
    [JsonPropertyName("endpointId")]
    public string EndpointId { get; init; } = string.Empty;

    /// <summary>The domain event this delivery carries (<c>nbo…evt</c>) — the dedupe key.</summary>
    [JsonPropertyName("eventId")]
    public string EventId { get; init; } = string.Empty;

    /// <summary>One of <c>pending</c>, <c>succeeded</c>, <c>failed</c>, <c>dead</c>.</summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    /// <summary>The number of delivery attempts so far.</summary>
    [JsonPropertyName("attempts")]
    public int Attempts { get; init; }

    /// <summary>When the next attempt is scheduled, or <c>null</c>.</summary>
    [JsonPropertyName("nextAttemptAt")]
    public DateTimeOffset? NextAttemptAt { get; init; }

    /// <summary>When the last attempt happened, or <c>null</c>.</summary>
    [JsonPropertyName("lastAttemptAt")]
    public DateTimeOffset? LastAttemptAt { get; init; }

    /// <summary>The HTTP status your endpoint returned, or <c>null</c>.</summary>
    [JsonPropertyName("responseStatus")]
    public int? ResponseStatus { get; init; }

    /// <summary>When this delivery was last replayed, or <c>null</c>.</summary>
    [JsonPropertyName("replayedAt")]
    public DateTimeOffset? ReplayedAt { get; init; }

    /// <summary>How many times this delivery was replayed.</summary>
    [JsonPropertyName("replayCount")]
    public int ReplayCount { get; init; }

    /// <summary>When the delivery record was created.</summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>Parameters for <see cref="WebhookEndpointsResource.CreateAsync"/>.</summary>
public sealed class WebhookEndpointCreateParams
{
    /// <summary>The endpoint URL.</summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>Defaults to <c>["*"]</c> (all events) server-side.</summary>
    [JsonPropertyName("enabledEvents")]
    public IReadOnlyList<string>? EnabledEvents { get; init; }
}

/// <summary>Parameters for <see cref="WebhookEndpointsResource.UpdateAsync"/>. At least one field must be provided.</summary>
public sealed class WebhookEndpointUpdateParams
{
    /// <summary>A new endpoint URL.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    /// <summary>A new set of enabled event types.</summary>
    [JsonPropertyName("enabledEvents")]
    public IReadOnlyList<string>? EnabledEvents { get; init; }

    /// <summary><c>true</c> pauses deliveries; <c>false</c> re-enables.</summary>
    [JsonPropertyName("disabled")]
    public bool? Disabled { get; init; }
}

/// <summary>Filters for <see cref="WebhookEndpointsResource.ListAsync"/>.</summary>
public sealed class WebhookEndpointListParams
{
    /// <summary>Page size, 1–100 (the API default is 20).</summary>
    public int? Limit { get; init; }

    /// <summary>Opaque cursor from a previous page's <c>Pagination.NextCursor</c>.</summary>
    public string? Cursor { get; init; }
}

/// <summary>Filters for <see cref="WebhookEndpointDeliveriesResource.ListAsync"/>.</summary>
public sealed class WebhookDeliveryListParams
{
    /// <summary>Filter by delivery status (<c>pending</c>, <c>succeeded</c>, <c>failed</c>, <c>dead</c>).</summary>
    public string? Status { get; init; }

    /// <summary>Filter by event type.</summary>
    public string? EventType { get; init; }

    /// <summary>Filter by endpoint.</summary>
    public string? Endpoint { get; init; }

    /// <summary>Page size, 1–100 (the API default is 20).</summary>
    public int? Limit { get; init; }

    /// <summary>Opaque cursor from a previous page's <c>Pagination.NextCursor</c>.</summary>
    public string? Cursor { get; init; }
}

/// <summary>Deliveries under an endpoint: inspect and replay.</summary>
public sealed class WebhookEndpointDeliveriesResource : NombaoneResource
{
    internal WebhookEndpointDeliveriesResource(Nombaone client)
        : base(client)
    {
    }

    /// <summary>List an endpoint's deliveries, newest first.</summary>
    /// <param name="endpointId">The endpoint id (<c>nbo…whk</c>).</param>
    /// <param name="parameters">Optional filters and page size.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<NombaonePage<WebhookDelivery>> ListAsync(string endpointId, WebhookDeliveryListParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAsync<WebhookDelivery>($"/webhooks/{Seg(endpointId)}/deliveries", BuildListQuery(parameters), options, cancellationToken);

    /// <summary>List an endpoint's deliveries as an async stream, fetching pages for you as you iterate.</summary>
    /// <param name="endpointId">The endpoint id (<c>nbo…whk</c>).</param>
    /// <param name="parameters">Optional filters and page size.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Stops iteration and cancels in-flight fetches.</param>
    public IAsyncEnumerable<WebhookDelivery> ListAutoPagingAsync(string endpointId, WebhookDeliveryListParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAutoPagingAsync<WebhookDelivery>($"/webhooks/{Seg(endpointId)}/deliveries", BuildListQuery(parameters), options, cancellationToken);

    /// <summary>Retrieve one delivery.</summary>
    /// <param name="endpointId">The endpoint id (<c>nbo…whk</c>).</param>
    /// <param name="deliveryId">The delivery id (<c>nbo…whd</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<WebhookDelivery> RetrieveAsync(string endpointId, string deliveryId, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        GetAsync<WebhookDelivery>($"/webhooks/{Seg(endpointId)}/deliveries/{Seg(deliveryId)}", options, cancellationToken);

    /// <summary>
    /// Redeliver a past delivery. The <b>original event id is kept</b>, so a
    /// receiver that dedupes on the event id will correctly treat it as
    /// already-seen.
    /// </summary>
    /// <param name="endpointId">The endpoint id (<c>nbo…whk</c>).</param>
    /// <param name="deliveryId">The delivery id (<c>nbo…whd</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<WebhookDelivery> ReplayAsync(string endpointId, string deliveryId, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<WebhookDelivery>($"/webhooks/{Seg(endpointId)}/deliveries/{Seg(deliveryId)}/replay", EmptyBody, options, cancellationToken);

    private static IReadOnlyDictionary<string, string?> BuildListQuery(WebhookDeliveryListParams? parameters) => new Dictionary<string, string?>
    {
        ["status"] = parameters?.Status,
        ["eventType"] = parameters?.EventType,
        ["endpoint"] = parameters?.Endpoint,
        ["limit"] = parameters?.Limit?.ToString(CultureInfo.InvariantCulture),
        ["cursor"] = parameters?.Cursor,
    };
}

/// <summary>
/// Webhook endpoints — register and manage the URLs that receive signed events.
/// (To <i>verify</i> incoming deliveries in your handler, use
/// <c>WebhookVerifier.ConstructEvent</c> — the crypto helper, not this REST resource.)
/// </summary>
public sealed class WebhookEndpointsResource : NombaoneResource
{
    internal WebhookEndpointsResource(Nombaone client)
        : base(client)
    {
        Deliveries = new WebhookEndpointDeliveriesResource(client);
    }

    /// <summary>Deliveries under an endpoint.</summary>
    public WebhookEndpointDeliveriesResource Deliveries { get; }

    /// <summary>
    /// Register an endpoint. The response includes the full <c>SigningSecret</c>
    /// <b>exactly once</b> — store it in your secret manager immediately.
    /// </summary>
    /// <param name="parameters">The URL and (optional) enabled events.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <example>
    /// <code>
    /// var endpoint = await nombaone.WebhookEndpoints.CreateAsync(new WebhookEndpointCreateParams
    /// {
    ///     Url = "https://example.com/nombaone/webhooks",
    ///     EnabledEvents = new[] { "invoice.paid", "invoice.payment_failed" },
    /// });
    /// await secrets.StoreAsync("NOMBAONE_WEBHOOK_SECRET", endpoint.SigningSecret);
    /// </code>
    /// </example>
    public Task<WebhookEndpointWithSecret> CreateAsync(WebhookEndpointCreateParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<WebhookEndpointWithSecret>("/webhooks", parameters, options, cancellationToken);

    /// <summary>Retrieve an endpoint by id.</summary>
    /// <param name="id">The endpoint id (<c>nbo…whk</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="NotFoundException">404 <c>WEBHOOK_ENDPOINT_NOT_FOUND</c>.</exception>
    public Task<WebhookEndpoint> RetrieveAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        GetAsync<WebhookEndpoint>($"/webhooks/{Seg(id)}", options, cancellationToken);

    /// <summary>Update url, event subscription, or enabled state.</summary>
    /// <param name="id">The endpoint id (<c>nbo…whk</c>).</param>
    /// <param name="parameters">The fields to change.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<WebhookEndpoint> UpdateAsync(string id, WebhookEndpointUpdateParams parameters, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PatchAsync<WebhookEndpoint>($"/webhooks/{Seg(id)}", parameters, options, cancellationToken);

    /// <summary>List your endpoints.</summary>
    /// <param name="parameters">Optional page size and cursor.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<NombaonePage<WebhookEndpoint>> ListAsync(WebhookEndpointListParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAsync<WebhookEndpoint>("/webhooks", BuildListQuery(parameters), options, cancellationToken);

    /// <summary>List your endpoints as an async stream, fetching pages for you as you iterate.</summary>
    /// <param name="parameters">Optional page size and cursor.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Stops iteration and cancels in-flight fetches.</param>
    public IAsyncEnumerable<WebhookEndpoint> ListAutoPagingAsync(WebhookEndpointListParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAutoPagingAsync<WebhookEndpoint>("/webhooks", BuildListQuery(parameters), options, cancellationToken);

    /// <summary>Delete an endpoint. Pending deliveries to it are retired.</summary>
    /// <param name="id">The endpoint id (<c>nbo…whk</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<WebhookEndpoint> DeleteAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        DeleteAsync<WebhookEndpoint>($"/webhooks/{Seg(id)}", options, cancellationToken);

    /// <summary>
    /// Rotate the signing secret. The new secret is returned <b>exactly once</b>;
    /// the old one is briefly honored so you can roll without dropping in-flight
    /// deliveries.
    /// </summary>
    /// <param name="id">The endpoint id (<c>nbo…whk</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<RotatedWebhookSecret> RotateSecretAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        PostAsync<RotatedWebhookSecret>($"/webhooks/{Seg(id)}/rotate-secret", EmptyBody, options, cancellationToken);

    private static IReadOnlyDictionary<string, string?> BuildListQuery(WebhookEndpointListParams? parameters) => new Dictionary<string, string?>
    {
        ["limit"] = parameters?.Limit?.ToString(CultureInfo.InvariantCulture),
        ["cursor"] = parameters?.Cursor,
    };
}
