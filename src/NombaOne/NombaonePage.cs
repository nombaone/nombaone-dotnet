using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NombaOne.Internal;

namespace NombaOne;

/// <summary>
/// One page of a list response, plus everything needed to keep going. Read this
/// page's items from <see cref="Data"/>, walk pages by hand with
/// <see cref="HasNextPage"/> and <see cref="NextPageAsync"/>, or iterate every
/// item across every page with <see cref="AutoPagingEachAsync"/> — cursors are
/// threaded for you and the original filters are preserved.
/// </summary>
/// <typeparam name="T">The list item type.</typeparam>
/// <example>
/// <code>
/// var page = await nombaone.Invoices.ListAsync(new InvoiceListParams { Status = "open" });
/// foreach (var invoice in page.Data) { /* this page */ }
///
/// await foreach (var invoice in nombaone.Invoices.ListAutoPagingAsync(new InvoiceListParams { Status = "open" }))
/// {
///     // every invoice across every page
/// }
/// </code>
/// </example>
public sealed class NombaonePage<T>
{
    private readonly Nombaone _client;
    private readonly RequestSpec _spec;

    internal NombaonePage(Nombaone client, RequestSpec spec, TransportResult<List<T>> result)
    {
        _client = client;
        _spec = spec;
        Data = result.Data;
        Pagination = result.Pagination ?? new NombaonePagination(result.Data.Count, hasMore: false, nextCursor: null);
        RawResponse = result.Response;

        // Give each list item the page's response metadata, so an item plucked
        // from Data still exposes its RequestId.
        foreach (var item in result.Data)
        {
            if (item is NombaoneEntity entity)
            {
                entity.RawResponse = result.Response;
            }
        }
    }

    /// <summary>The items on this page.</summary>
    public IReadOnlyList<T> Data { get; }

    /// <summary>The cursor block: <c>Limit</c>, <c>HasMore</c>, <c>NextCursor</c>.</summary>
    public NombaonePagination Pagination { get; }

    /// <summary>Response metadata (status, request id, headers) for this page's fetch.</summary>
    public NombaoneResponse RawResponse { get; }

    /// <summary>The request id for this page's fetch.</summary>
    public string? RequestId => RawResponse.RequestId;

    /// <summary>Whether another page exists after this one.</summary>
    public bool HasNextPage => Pagination.HasMore && !string.IsNullOrEmpty(Pagination.NextCursor);

    /// <summary>
    /// Fetch the next page, threading the cursor while preserving the original
    /// filters. Guard with <see cref="HasNextPage"/> first.
    /// </summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="InvalidOperationException">There is no next page.</exception>
    public async Task<NombaonePage<T>> NextPageAsync(CancellationToken cancellationToken = default)
    {
        if (!HasNextPage)
        {
            throw new InvalidOperationException(
                "No next page available — check HasNextPage before calling NextPageAsync().");
        }

        var nextSpec = _spec.WithQueryParam("cursor", Pagination.NextCursor!);
        return await CreateAsync(_client, nextSpec, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Iterate every item across this page and all subsequent pages. Cursors are
    /// threaded automatically.
    /// </summary>
    /// <param name="cancellationToken">Stops iteration and cancels in-flight fetches.</param>
    public async IAsyncEnumerable<T> AutoPagingEachAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var page = this;
        while (true)
        {
            foreach (var item in page.Data)
            {
                yield return item;
            }

            if (!page.HasNextPage)
            {
                yield break;
            }

            page = await page.NextPageAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    internal static async Task<NombaonePage<T>> CreateAsync(Nombaone client, RequestSpec spec, CancellationToken cancellationToken)
    {
        var result = await client.SendRawAsync<List<T>>(spec, cancellationToken).ConfigureAwait(false);
        return new NombaonePage<T>(client, spec, result);
    }
}
