using RaceCountdown.Core.Feed;

namespace RaceCountdown.Core.Tests;

public sealed class FeedCacheTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "RaceCountdownTests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Empty_cache_has_no_feed_and_empty_state()
    {
        var cache = new FeedCache(directory);

        Assert.Null(await cache.LoadFeedAsync());
        Assert.Equal(FeedCacheState.Empty, await cache.LoadStateAsync());
    }

    [Fact]
    public async Task Round_trips_the_feed_and_state()
    {
        var cache = new FeedCache(directory);
        var feed = TestFeeds.Feed(TestFeeds.Bathurst2026());
        var at = new DateTimeOffset(2026, 9, 27, 1, 2, 3, TimeSpan.Zero);
        var state = new FeedCacheState("\"abc\"", at, at.AddMinutes(5), LastRefreshFailed: true);

        await cache.SaveFeedAsync(feed);
        await cache.SaveStateAsync(state);

        var loaded = await cache.LoadFeedAsync();
        Assert.Equal(FeedSerializer.Serialize(feed), FeedSerializer.Serialize(loaded!));
        Assert.Equal(state, await cache.LoadStateAsync());
        Assert.False(File.Exists(Path.Combine(directory, FeedCache.FeedFileName + ".tmp")));
    }

    [Fact]
    public async Task Writes_state_as_readable_json()
    {
        var cache = new FeedCache(directory);
        await cache.SaveStateAsync(new FeedCacheState("\"abc\"", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)));

        var json = await File.ReadAllTextAsync(Path.Combine(directory, FeedCache.StateFileName));

        Assert.Contains("\"lastSuccessUtc\":\"2026-01-01T00:00:00Z\"", json);
        Assert.Contains("\"lastAttemptUtc\":null", json);
    }

    [Theory]
    [InlineData("garbage")]
    [InlineData("{\"schemaVersion\": 1}")]
    public async Task Damaged_feed_reads_as_missing(string contents)
    {
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, FeedCache.FeedFileName), contents);

        Assert.Null(await new FeedCache(directory).LoadFeedAsync());
    }

    [Fact]
    public async Task Feed_that_fails_validation_reads_as_missing()
    {
        Directory.CreateDirectory(directory);
        var bad = TestFeeds.Feed(TestFeeds.Bathurst2026()) with { SchemaVersion = 99 };
        await File.WriteAllTextAsync(Path.Combine(directory, FeedCache.FeedFileName), FeedSerializer.Serialize(bad));

        Assert.Null(await new FeedCache(directory).LoadFeedAsync());
    }

    [Theory]
    [InlineData("garbage")]
    [InlineData("null")]
    public async Task Damaged_state_reads_as_empty(string contents)
    {
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, FeedCache.StateFileName), contents);

        Assert.Equal(FeedCacheState.Empty, await new FeedCache(directory).LoadStateAsync());
    }
}
