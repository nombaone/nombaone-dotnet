using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NombaOne.Internal;

namespace NombaOne;

/// <summary>
/// The base type every resource namespace derives from. It has no public
/// surface of its own; use the resource namespaces on <see cref="Nombaone"/>
/// (for example <c>nombaone.Customers</c>).
/// </summary>
public abstract class NombaoneResource
{
    private protected NombaoneResource(Nombaone client) => Client = client;

    private protected Nombaone Client { get; }

    /// <summary>A body for bodyless POSTs — serializes to <c>{}</c>, matching the API's expectation.</summary>
    private protected static readonly object EmptyBody = new();

    /// <summary>Percent-encode one path segment (ids come from user input — never trust raw).</summary>
    private protected static string Seg(string value) => Uri.EscapeDataString(value);

    private protected Task<T> GetAsync<T>(string path, RequestOptions? options, CancellationToken cancellationToken) =>
        Client.SendAsync<T>(new RequestSpec(HttpVerbs.Get, path, options: options), cancellationToken);

    private protected Task<T> PostAsync<T>(string path, object? body, RequestOptions? options, CancellationToken cancellationToken) =>
        Client.SendAsync<T>(new RequestSpec(HttpVerbs.Post, path, body: body, options: options), cancellationToken);

    private protected Task<T> PatchAsync<T>(string path, object? body, RequestOptions? options, CancellationToken cancellationToken) =>
        Client.SendAsync<T>(new RequestSpec(HttpVerbs.Patch, path, body: body, options: options), cancellationToken);

    private protected Task<T> PutAsync<T>(string path, object? body, RequestOptions? options, CancellationToken cancellationToken) =>
        Client.SendAsync<T>(new RequestSpec(HttpVerbs.Put, path, body: body, options: options), cancellationToken);

    private protected Task<T> DeleteAsync<T>(string path, RequestOptions? options, CancellationToken cancellationToken) =>
        Client.SendAsync<T>(new RequestSpec(HttpVerbs.Delete, path, options: options), cancellationToken);

    private protected Task<NombaonePage<T>> ListAsync<T>(
        string path,
        IReadOnlyDictionary<string, string?>? query,
        RequestOptions? options,
        CancellationToken cancellationToken) =>
        NombaonePage<T>.CreateAsync(Client, new RequestSpec(HttpVerbs.Get, path, query: query, options: options), cancellationToken);

    private protected async IAsyncEnumerable<T> ListAutoPagingAsync<T>(
        string path,
        IReadOnlyDictionary<string, string?>? query,
        RequestOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var page = await NombaonePage<T>
            .CreateAsync(Client, new RequestSpec(HttpVerbs.Get, path, query: query, options: options), cancellationToken)
            .ConfigureAwait(false);

        await foreach (var item in page.AutoPagingEachAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }
}
