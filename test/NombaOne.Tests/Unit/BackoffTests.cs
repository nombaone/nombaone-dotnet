using System;
using System.Net.Http;
using System.Net.Http.Headers;
using NombaOne.Internal;

namespace NombaOne.Tests.Unit;

public class BackoffTests
{
    private static HttpResponseHeaders Headers(Action<HttpResponseHeaders> configure)
    {
        var response = new HttpResponseMessage();
        configure(response.Headers);
        return response.Headers;
    }

    [Fact]
    public void RetryAfter_parses_delta_seconds()
    {
        var headers = Headers(h => h.TryAddWithoutValidation("Retry-After", "5"));
        Assert.Equal(TimeSpan.FromSeconds(5), Backoff.RetryAfter(headers));
    }

    [Fact]
    public void RetryAfter_parses_http_date_in_the_future()
    {
        var future = DateTimeOffset.UtcNow.AddMinutes(2).ToString("R");
        var headers = Headers(h => h.TryAddWithoutValidation("Retry-After", future));

        var delay = Backoff.RetryAfter(headers);

        Assert.NotNull(delay);
        Assert.True(delay!.Value > TimeSpan.Zero);
        Assert.True(delay.Value <= TimeSpan.FromMinutes(2.1));
    }

    [Fact]
    public void RetryAfter_is_null_when_absent()
    {
        Assert.Null(Backoff.RetryAfter(Headers(_ => { })));
    }

    [Fact]
    public void IntHeaderValue_reads_and_tolerates_missing()
    {
        var headers = Headers(h => h.TryAddWithoutValidation("X-RateLimit-Limit", "100"));
        Assert.Equal(100, Backoff.IntHeaderValue(headers, "X-RateLimit-Limit"));
        Assert.Null(Backoff.IntHeaderValue(headers, "X-RateLimit-Remaining"));
    }

    [Fact]
    public void FullJitter_stays_within_bounds()
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var ceiling = Math.Min(8_000, 500 * Math.Pow(2, attempt));
            for (var i = 0; i < 50; i++)
            {
                var delay = Backoff.FullJitter(attempt).TotalMilliseconds;
                Assert.True(delay >= 0);
                Assert.True(delay <= ceiling);
            }
        }
    }
}
