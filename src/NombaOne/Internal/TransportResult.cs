namespace NombaOne.Internal;

/// <summary>What the transport hands back to a resource method after a call.</summary>
/// <typeparam name="T">The unwrapped resource (or list) type.</typeparam>
internal sealed class TransportResult<T>
{
    internal TransportResult(T data, NombaonePagination? pagination, NombaoneResponse response)
    {
        Data = data;
        Pagination = pagination;
        Response = response;
    }

    /// <summary>The unwrapped <c>data</c>.</summary>
    internal T Data { get; }

    /// <summary>The pagination block, present only on list responses.</summary>
    internal NombaonePagination? Pagination { get; }

    /// <summary>The response metadata (status, request id, headers).</summary>
    internal NombaoneResponse Response { get; }
}
