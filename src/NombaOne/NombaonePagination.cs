namespace NombaOne;

/// <summary>
/// The cursor-pagination block returned at the top level of every list
/// response. Pagination is forward-only and cursor-based — there are no total
/// counts.
/// </summary>
public sealed class NombaonePagination
{
    /// <summary>The page size that was applied (1–100; the API default is 20).</summary>
    public int Limit { get; }

    /// <summary>Whether more items exist beyond this page.</summary>
    public bool HasMore { get; }

    /// <summary>The opaque cursor for the next page, or <c>null</c> when <see cref="HasMore"/> is false.</summary>
    public string? NextCursor { get; }

    internal NombaonePagination(int limit, bool hasMore, string? nextCursor)
    {
        Limit = limit;
        HasMore = hasMore;
        NextCursor = nextCursor;
    }
}
