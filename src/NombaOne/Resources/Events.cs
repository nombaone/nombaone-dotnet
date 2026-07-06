using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NombaOne;

/// <summary>One catalog entry: when the event fires and which <c>data</c> keys it carries.</summary>
public sealed class EventCatalogEntry
{
    /// <summary>A description of the transition that produces this event.</summary>
    [JsonPropertyName("when")]
    public string When { get; init; } = string.Empty;

    /// <summary>The keys present on the event's <c>data</c> object.</summary>
    [JsonPropertyName("payload")]
    public IReadOnlyList<string> Payload { get; init; } = System.Array.Empty<string>();
}

/// <summary>Filters for <see cref="EventsResource.ListAsync"/>.</summary>
public sealed class EventListParams
{
    /// <summary>Filter to one catalog type, e.g. <c>invoice.paid</c>.</summary>
    public string? Type { get; init; }

    /// <summary>Page size, 1–100 (the API default is 20).</summary>
    public int? Limit { get; init; }

    /// <summary>Opaque cursor from a previous page's <c>Pagination.NextCursor</c>.</summary>
    public string? Cursor { get; init; }
}

/// <summary>
/// Events — the append-only log behind every webhook. Webhook delivery is
/// at-least-once; this log is your reconciliation backstop when a delivery was
/// missed or you need to backfill.
/// </summary>
public sealed class EventsResource : NombaoneResource
{
    internal EventsResource(Nombaone client)
        : base(client)
    {
    }

    /// <summary>List events, newest first.</summary>
    /// <param name="parameters">Optional filters and page size.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <example>
    /// <code>
    /// await foreach (var evt in nombaone.Events.ListAutoPagingAsync(new EventListParams { Type = "invoice.paid" }))
    /// {
    ///     Console.WriteLine($"{evt.Id} {evt.Type}");
    /// }
    /// </code>
    /// </example>
    public Task<NombaonePage<DomainEvent>> ListAsync(EventListParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAsync<DomainEvent>("/events", BuildListQuery(parameters), options, cancellationToken);

    /// <summary>List events as an async stream, fetching pages for you as you iterate.</summary>
    /// <param name="parameters">Optional filters and page size.</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Stops iteration and cancels in-flight fetches.</param>
    public IAsyncEnumerable<DomainEvent> ListAutoPagingAsync(EventListParams? parameters = null, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        ListAutoPagingAsync<DomainEvent>("/events", BuildListQuery(parameters), options, cancellationToken);

    /// <summary>Retrieve one event by id (<c>nbo…evt</c>).</summary>
    /// <param name="id">The event id (<c>nbo…evt</c>).</param>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="NotFoundException">404 <c>WEBHOOK_EVENT_NOT_FOUND</c>.</exception>
    public Task<DomainEvent> RetrieveAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        GetAsync<DomainEvent>($"/events/{Seg(id)}", options, cancellationToken);

    /// <summary>
    /// The machine-readable event catalog — every event type the platform can
    /// emit, with a description and its <c>data</c> keys.
    /// </summary>
    /// <param name="options">Per-call options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<IReadOnlyDictionary<string, EventCatalogEntry>> CatalogAsync(RequestOptions? options = null, CancellationToken cancellationToken = default) =>
        await GetAsync<Dictionary<string, EventCatalogEntry>>("/events/catalog", options, cancellationToken).ConfigureAwait(false);

    private static IReadOnlyDictionary<string, string?> BuildListQuery(EventListParams? parameters) => new Dictionary<string, string?>
    {
        ["type"] = parameters?.Type,
        ["limit"] = parameters?.Limit?.ToString(CultureInfo.InvariantCulture),
        ["cursor"] = parameters?.Cursor,
    };
}
