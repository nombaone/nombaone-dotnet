using System;
using System.Collections.Generic;
using System.Text;

namespace NombaOne.Internal;

/// <summary>
/// Serializes list-filter parameters into a query string. <c>null</c> values
/// are dropped (an omitted filter, not an empty one); everything else is
/// percent-encoded. Returns <c>""</c> or a string starting with <c>?</c>.
/// </summary>
internal static class QueryString
{
    internal static string Build(IReadOnlyDictionary<string, string?>? query)
    {
        if (query is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var pair in query)
        {
            if (pair.Value is null)
            {
                continue;
            }

            builder.Append(builder.Length == 0 ? '?' : '&');
            builder.Append(Uri.EscapeDataString(pair.Key));
            builder.Append('=');
            builder.Append(Uri.EscapeDataString(pair.Value));
        }

        return builder.ToString();
    }
}
