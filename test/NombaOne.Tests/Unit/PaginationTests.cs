using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NombaOne;
using NombaOne.Internal;
using NombaOne.Tests.Helpers;

namespace NombaOne.Tests.Unit;

public class PaginationTests
{
    private static string PageJson(IEnumerable<string> ids, string? nextCursor)
    {
        var data = string.Join(",", ids.Select(id => $"{{\"id\":\"{id}\",\"amountInKobo\":1}}"));
        var hasMore = nextCursor is not null;
        var cursor = nextCursor is null ? "null" : $"\"{nextCursor}\"";
        return "{\"success\":true,\"statusCode\":200,\"data\":[" + data + "]," +
               $"\"pagination\":{{\"limit\":2,\"hasMore\":{(hasMore ? "true" : "false")},\"nextCursor\":{cursor}}}," +
               "\"meta\":{\"requestId\":\"req_page\"}}";
    }

    private static string? QueryValue(Uri uri, string key)
    {
        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var index = pair.IndexOf('=');
            if (index < 0)
            {
                continue;
            }

            if (Uri.UnescapeDataString(pair.Substring(0, index)) == key)
            {
                return Uri.UnescapeDataString(pair.Substring(index + 1));
            }
        }

        return null;
    }

    private static (Nombaone Client, MockHttpHandler Handler) ThreePageClient()
    {
        var handler = new MockHttpHandler((req, _, _) =>
        {
            var cursor = QueryValue(req.RequestUri!, "cursor");
            var json = cursor switch
            {
                null => PageJson(new[] { "nbo1", "nbo2" }, "cur_2"),
                "cur_2" => PageJson(new[] { "nbo3", "nbo4" }, "cur_3"),
                "cur_3" => PageJson(new[] { "nbo5" }, null),
                _ => throw new InvalidOperationException($"unexpected cursor {cursor}"),
            };
            return Task.FromResult(Responses.Json(HttpStatusCode.OK, json));
        });

        var client = new Nombaone(new NombaoneOptions
        {
            ApiKey = "nbo_sandbox_test",
            BaseUrl = "https://api.test",
            HttpClient = new HttpClient(handler),
        });
        return (client, handler);
    }

    private static RequestSpec ListSpec() =>
        new(HttpVerbs.Get, "/customers", query: new Dictionary<string, string?> { ["status"] = "open" });

    [Fact]
    public async Task Manual_paging_walks_all_three_pages()
    {
        var (client, _) = ThreePageClient();

        var page1 = await NombaonePage<TestResource>.CreateAsync(client, ListSpec(), CancellationToken.None);
        Assert.Equal(new[] { "nbo1", "nbo2" }, page1.Data.Select(r => r.Id));
        Assert.True(page1.HasNextPage);
        Assert.Equal("cur_2", page1.Pagination.NextCursor);

        var page2 = await page1.NextPageAsync();
        Assert.Equal(new[] { "nbo3", "nbo4" }, page2.Data.Select(r => r.Id));
        Assert.True(page2.HasNextPage);

        var page3 = await page2.NextPageAsync();
        Assert.Equal(new[] { "nbo5" }, page3.Data.Select(r => r.Id));
        Assert.False(page3.HasNextPage);
    }

    [Fact]
    public async Task Auto_paging_yields_every_item_across_pages_in_order()
    {
        var (client, _) = ThreePageClient();

        var page1 = await NombaonePage<TestResource>.CreateAsync(client, ListSpec(), CancellationToken.None);

        var ids = new List<string>();
        await foreach (var item in page1.AutoPagingEachAsync())
        {
            ids.Add(item.Id);
        }

        Assert.Equal(new[] { "nbo1", "nbo2", "nbo3", "nbo4", "nbo5" }, ids);
    }

    [Fact]
    public async Task Threads_the_cursor_and_preserves_the_original_filter()
    {
        var (client, handler) = ThreePageClient();

        var page1 = await NombaonePage<TestResource>.CreateAsync(client, ListSpec(), CancellationToken.None);
        await foreach (var _ in page1.AutoPagingEachAsync())
        {
        }

        Assert.Equal(3, handler.Requests.Count);
        Assert.All(handler.Requests, r => Assert.Contains("status=open", r.Url));
        Assert.DoesNotContain("cursor", handler.Requests[0].Url);
        Assert.Contains("cursor=cur_2", handler.Requests[1].Url);
        Assert.Contains("cursor=cur_3", handler.Requests[2].Url);
    }

    [Fact]
    public async Task NextPage_throws_when_there_is_no_next_page()
    {
        var handler = new MockHttpHandler((_, _, _) =>
            Task.FromResult(Responses.Json(HttpStatusCode.OK, PageJson(new[] { "only" }, nextCursor: null))));
        var client = new Nombaone(new NombaoneOptions
        {
            ApiKey = "nbo_sandbox_test",
            BaseUrl = "https://api.test",
            HttpClient = new HttpClient(handler),
        });

        var page = await NombaonePage<TestResource>.CreateAsync(client, ListSpec(), CancellationToken.None);

        Assert.False(page.HasNextPage);
        await Assert.ThrowsAsync<InvalidOperationException>(() => page.NextPageAsync());
    }

    [Fact]
    public async Task List_items_carry_the_page_request_id()
    {
        var (client, _) = ThreePageClient();

        var page = await NombaonePage<TestResource>.CreateAsync(client, ListSpec(), CancellationToken.None);

        Assert.Equal("req_page", page.RequestId);
        Assert.All(page.Data, item => Assert.Equal("req_page", item.RequestId));
    }
}
