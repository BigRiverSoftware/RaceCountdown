using System.Text;
using Microsoft.Extensions.Time.Testing;
using RaceCountdown.Core.Countdown;
using RaceCountdown.Core.Feed;
using RaceCountdown.Core.Models;

namespace RaceCountdown.Core.Tests;

public sealed class FeedStoreTests : IDisposable
{
    private static readonly DateTimeOffset RaceStart2027 = new(2027, 10, 10, 0, 30, 0, TimeSpan.Zero);

    private readonly string directory = Path.Combine(Path.GetTempPath(), "RaceCountdownTests", Guid.NewGuid().ToString("N"));
    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 9, 28, 0, 0, 0, TimeSpan.Zero));
    private readonly FakeClient client = new();

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private FeedCache Cache => new(directory);

    private FeedStore CreateStore(EventFeed? bundled) =>
        new(Cache, client, _ => bundled is null
            ? throw new FileNotFoundException("events.snapshot.json")
            : Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(FeedSerializer.Serialize(bundled)))), time);

    private static EventFeed FeedAt(DateTimeOffset generated, params RaceEvent[] events) =>
        TestFeeds.Feed(generated, events.Length == 0 ? [TestFeeds.Bathurst2026()] : events);

    [Fact]
    public async Task Cold_start_offline_uses_the_bundled_snapshot()
    {
        var bundled = FeedAt(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero));
        var store = CreateStore(bundled);

        await store.LoadAsync();

        Assert.Equal(bundled.GeneratedUtc, store.Feed!.GeneratedUtc);
        Assert.Equal(FeedCacheState.Empty, store.State);
    }

    [Fact]
    public async Task Cache_wins_when_newer_than_the_snapshot()
    {
        await Cache.SaveFeedAsync(FeedAt(new DateTimeOffset(2026, 9, 20, 0, 0, 0, TimeSpan.Zero)));
        var store = CreateStore(FeedAt(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero)));

        await store.LoadAsync();

        Assert.Equal(20, store.Feed!.GeneratedUtc.Day);
    }

    [Fact]
    public async Task Snapshot_wins_when_newer_than_the_cache()
    {
        await Cache.SaveFeedAsync(FeedAt(new DateTimeOffset(2025, 9, 20, 0, 0, 0, TimeSpan.Zero)));
        var store = CreateStore(FeedAt(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero)));

        await store.LoadAsync();

        Assert.Equal(2026, store.Feed!.GeneratedUtc.Year);
    }

    [Fact]
    public async Task No_cache_and_no_snapshot_leaves_no_feed()
    {
        var store = CreateStore(bundled: null);

        await store.LoadAsync();

        Assert.Null(store.Feed);
    }

    [Fact]
    public async Task Invalid_snapshot_is_ignored()
    {
        var store = CreateStore(FeedAt(time.GetUtcNow()) with { SchemaVersion = 99 });

        await store.LoadAsync();

        Assert.Null(store.Feed);
    }

    [Fact]
    public async Task Refresh_saves_a_new_feed_and_state()
    {
        var store = CreateStore(FeedAt(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero)));
        await store.LoadAsync();
        var downloaded = FeedAt(new DateTimeOffset(2026, 9, 27, 0, 0, 0, TimeSpan.Zero));
        client.Results.Enqueue(new FeedFetchResult(FeedFetchStatus.Updated, downloaded, "\"v2\""));
        var changed = 0;
        store.Changed += (_, _) => changed++;

        var status = await store.RefreshAsync();

        Assert.Equal(FeedFetchStatus.Updated, status);
        Assert.Same(downloaded, store.Feed);
        Assert.Equal(new FeedCacheState("\"v2\"", time.GetUtcNow(), time.GetUtcNow()), store.State);
        Assert.Equal(1, changed);
        Assert.Equal(downloaded.GeneratedUtc, (await Cache.LoadFeedAsync())!.GeneratedUtc);
        Assert.Equal(store.State, await Cache.LoadStateAsync());
    }

    [Fact]
    public async Task Refresh_does_not_go_back_to_an_older_feed()
    {
        var store = CreateStore(FeedAt(new DateTimeOffset(2026, 9, 27, 0, 0, 0, TimeSpan.Zero)));
        await store.LoadAsync();
        client.Results.Enqueue(new FeedFetchResult(FeedFetchStatus.Updated, FeedAt(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero)), "\"old\""));

        await store.RefreshAsync();

        Assert.Equal(27, store.Feed!.GeneratedUtc.Day);
        Assert.False(store.State.LastRefreshFailed);
    }

    [Fact]
    public async Task Sends_the_etag_only_for_a_cached_feed()
    {
        await Cache.SaveFeedAsync(FeedAt(new DateTimeOffset(2026, 9, 20, 0, 0, 0, TimeSpan.Zero)));
        await Cache.SaveStateAsync(new FeedCacheState("\"cached\""));
        var cachedStore = CreateStore(bundled: null);
        await cachedStore.LoadAsync();
        client.Results.Enqueue(FeedFetchResult.NotModified);
        await cachedStore.RefreshAsync();

        // Same saved ETag, but a newer bundled snapshot is showing, so the ETag no longer describes it.
        var bundledStore = CreateStore(FeedAt(new DateTimeOffset(2026, 9, 25, 0, 0, 0, TimeSpan.Zero)));
        await bundledStore.LoadAsync();
        client.Results.Enqueue(FeedFetchResult.NotModified);
        await bundledStore.RefreshAsync();

        Assert.Equal(["\"cached\"", null], client.ETags);
    }

    [Fact]
    public async Task Not_modified_records_success()
    {
        await Cache.SaveStateAsync(new FeedCacheState("\"v1\"", LastRefreshFailed: true));
        var store = CreateStore(FeedAt(time.GetUtcNow()));
        await store.LoadAsync();
        client.Results.Enqueue(FeedFetchResult.NotModified);

        var status = await store.RefreshAsync();

        Assert.Equal(FeedFetchStatus.NotModified, status);
        Assert.Equal(new FeedCacheState("\"v1\"", time.GetUtcNow(), time.GetUtcNow(), false), store.State);
    }

    [Fact]
    public async Task Failure_keeps_the_feed_and_marks_the_refresh_failed()
    {
        var bundled = FeedAt(time.GetUtcNow());
        var store = CreateStore(bundled);
        await store.LoadAsync();
        client.Results.Enqueue(FeedFetchResult.Fail("offline"));

        var status = await store.RefreshAsync();

        Assert.Equal(FeedFetchStatus.Failed, status);
        Assert.Equal(bundled.GeneratedUtc, store.Feed!.GeneratedUtc);
        Assert.True(store.State.LastRefreshFailed);
        Assert.Null(store.State.LastSuccessUtc);
        Assert.Equal(time.GetUtcNow(), store.State.LastAttemptUtc);
    }

    [Fact]
    public async Task Rolls_over_to_next_years_race_after_the_race_finishes()
    {
        var feed = FeedAt(
            new DateTimeOffset(2026, 9, 27, 0, 0, 0, TimeSpan.Zero),
            TestFeeds.Bathurst2026(),
            TestFeeds.Bathurst(2027, RaceStart2027));
        var store = CreateStore(feed);
        await store.LoadAsync();
        client.Results.Enqueue(FeedFetchResult.NotModified);
        await store.RefreshAsync();
        var filter = EventFilter.Bathurst1000MainRace;

        CountdownState Now() => CountdownCalculator.Calculate(store.Feed!, filter, time);

        Assert.Equal(CountdownPhase.Counting, Now().Phase);
        Assert.Equal(TestFeeds.RaceStart2026, Now().Session!.StartUtc);

        time.SetUtcNow(TestFeeds.RaceStart2026.AddSeconds(1));
        Assert.Equal(CountdownPhase.Live, Now().Phase);
        Assert.True(RefreshPolicy.IsDue(store.Feed, filter, store.State, time.GetUtcNow()), "refresh straight after the start");

        time.SetUtcNow(TestFeeds.RaceStart2026 + TimeSpan.FromHours(7));
        Assert.Equal(CountdownPhase.Counting, Now().Phase);
        Assert.Equal(RaceStart2027, Now().Session!.StartUtc);
    }

    private sealed class FakeClient : IEventFeedClient
    {
        public Queue<FeedFetchResult> Results { get; } = new();

        public List<string?> ETags { get; } = [];

        public Task<FeedFetchResult> FetchAsync(string? etag, CancellationToken cancellationToken = default)
        {
            ETags.Add(etag);
            return Task.FromResult(Results.Dequeue());
        }
    }
}
