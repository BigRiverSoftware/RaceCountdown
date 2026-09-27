using System.Net;
using System.Text;
using RaceCountdown.Core.Feed;

namespace RaceCountdown.Core.Tests;

public class EventFeedClientTests
{
    private static readonly Uri FeedUrl = new("https://example.test/events.json");

    private static string ValidJson => FeedSerializer.Serialize(TestFeeds.Feed(TestFeeds.Bathurst2026()));

    private static (EventFeedClient Client, StubHandler Handler) Create(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond, TimeSpan? timeout = null)
    {
        var handler = new StubHandler(respond);
        var http = new HttpClient(handler) { Timeout = timeout ?? TimeSpan.FromSeconds(30) };
        return (new EventFeedClient(http, FeedUrl), handler);
    }

    private static HttpResponseMessage Json(string json, string? etag = null)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        if (etag is not null)
        {
            response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue(etag);
        }

        return response;
    }

    [Fact]
    public async Task Ok_returns_the_feed_and_its_etag()
    {
        var (client, handler) = Create((_, _) => Task.FromResult(Json(ValidJson, "\"v1\"")));

        var result = await client.FetchAsync(etag: null);

        Assert.Equal(FeedFetchStatus.Updated, result.Status);
        Assert.Equal("\"v1\"", result.ETag);
        Assert.Single(result.Feed!.Events);
        Assert.Equal(FeedUrl, handler.Requests.Single().RequestUri);
        Assert.Empty(handler.Requests.Single().Headers.IfNoneMatch);
    }

    [Fact]
    public async Task Sends_the_etag_and_reports_not_modified()
    {
        var (client, handler) = Create((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotModified)));

        var result = await client.FetchAsync("\"v1\"");

        Assert.Equal(FeedFetchStatus.NotModified, result.Status);
        Assert.Equal("\"v1\"", handler.Requests.Single().Headers.IfNoneMatch.Single().Tag);
    }

    [Fact]
    public async Task Ignores_a_malformed_etag()
    {
        var (client, handler) = Create((_, _) => Task.FromResult(Json(ValidJson)));

        var result = await client.FetchAsync("not quoted");

        Assert.Equal(FeedFetchStatus.Updated, result.Status);
        Assert.Null(result.ETag);
        Assert.Empty(handler.Requests.Single().Headers.IfNoneMatch);
    }

    [Fact]
    public async Task Server_error_fails()
    {
        var (client, _) = Create((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        var result = await client.FetchAsync(null);

        Assert.Equal(FeedFetchStatus.Failed, result.Status);
        Assert.Contains("503", result.Error);
    }

    [Fact]
    public async Task Network_error_fails()
    {
        var (client, _) = Create((_, _) => throw new HttpRequestException("No route to host"));

        var result = await client.FetchAsync(null);

        Assert.Equal(FeedFetchStatus.Failed, result.Status);
        Assert.Contains("No route to host", result.Error);
    }

    [Fact]
    public async Task Timeout_fails()
    {
        var (client, _) = Create(
            async (_, ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return Json(ValidJson);
            },
            TimeSpan.FromMilliseconds(50));

        var result = await client.FetchAsync(null);

        Assert.Equal(FeedFetchStatus.Failed, result.Status);
        Assert.Contains("timed out", result.Error);
    }

    [Fact]
    public async Task Caller_cancellation_is_not_swallowed()
    {
        var (client, _) = Create(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return Json(ValidJson);
        });
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.FetchAsync(null, cts.Token));
    }

    [Theory]
    [InlineData("<html>Not found</html>")]
    [InlineData("{\"schemaVersion\": 1}")]
    [InlineData("")]
    public async Task Malformed_json_fails(string body)
    {
        var (client, _) = Create((_, _) => Task.FromResult(Json(body)));

        var result = await client.FetchAsync(null);

        Assert.Equal(FeedFetchStatus.Failed, result.Status);
        Assert.Contains("not valid JSON", result.Error);
    }

    [Fact]
    public async Task Feed_that_fails_validation_fails()
    {
        var bad = TestFeeds.Feed(TestFeeds.Bathurst2026()) with { SchemaVersion = 99 };
        var (client, _) = Create((_, _) => Task.FromResult(Json(FeedSerializer.Serialize(bad))));

        var result = await client.FetchAsync(null);

        Assert.Equal(FeedFetchStatus.Failed, result.Status);
        Assert.Contains("schemaVersion", result.Error);
    }

    internal sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return respond(request, cancellationToken);
        }
    }
}
