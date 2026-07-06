using System;
using System.Threading.Tasks;
using NombaOne;

namespace NombaOne.Examples;

/// <summary>One page, manual cursor navigation, and auto-paging.</summary>
internal static class Pagination
{
    internal static async Task RunAsync()
    {
        using var nombaone = new Nombaone();
        var tag = Guid.NewGuid().ToString("N").Substring(0, 8);

        // Seed a few customers so paging has something to walk.
        for (var i = 0; i < 5; i++)
        {
            await nombaone.Customers.CreateAsync(new CustomerCreateParams
            {
                Email = $"page-{tag}-{i}@example.com",
                Name = $"Page {i}",
            });
        }

        // One page.
        var page = await nombaone.Customers.ListAsync(new CustomerListParams { Limit = 2 });
        Console.WriteLine($"page 1: {page.Data.Count} items, hasMore={page.Pagination.HasMore}");

        // Manual next page.
        if (page.HasNextPage)
        {
            var next = await page.NextPageAsync();
            Console.WriteLine($"page 2: {next.Data.Count} items, hasMore={next.Pagination.HasMore}");
        }

        // Auto-paging — cursors threaded for you.
        var seen = 0;
        await foreach (var customer in nombaone.Customers.ListAutoPagingAsync(new CustomerListParams { Limit = 2 }))
        {
            _ = customer;
            if (++seen >= 10)
            {
                break; // bound the demo
            }
        }

        Console.WriteLine($"auto-paged {seen} customers across pages");
    }
}
