using System.Collections.Generic;
using System.Net.Http;

namespace NombaOne.Internal;

/// <summary>The internal description of one HTTP call. Resource methods produce these.</summary>
internal sealed class RequestSpec
{
    internal RequestSpec(
        HttpMethod method,
        string path,
        IReadOnlyDictionary<string, string?>? query = null,
        object? body = null,
        RequestOptions? options = null)
    {
        Method = method;
        Path = path;
        Query = query;
        Body = body;
        Options = options;
    }

    /// <summary>The HTTP method.</summary>
    internal HttpMethod Method { get; }

    /// <summary>The path below <c>/v1</c>, with any id segments already percent-encoded.</summary>
    internal string Path { get; }

    /// <summary>Query parameters; <c>null</c> values are dropped when building the URL.</summary>
    internal IReadOnlyDictionary<string, string?>? Query { get; }

    /// <summary>The request body, serialized to JSON, or <c>null</c> for no body.</summary>
    internal object? Body { get; }

    /// <summary>Per-call options.</summary>
    internal RequestOptions? Options { get; }
}

/// <summary>Shared <see cref="HttpMethod"/> instances, including a PATCH verb that netstandard2.0 lacks.</summary>
internal static class HttpVerbs
{
    internal static readonly HttpMethod Get = HttpMethod.Get;
    internal static readonly HttpMethod Post = HttpMethod.Post;
    internal static readonly HttpMethod Put = HttpMethod.Put;
    internal static readonly HttpMethod Delete = HttpMethod.Delete;
    internal static readonly HttpMethod Patch = new HttpMethod("PATCH");
}
