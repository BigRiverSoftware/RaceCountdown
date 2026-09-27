using RaceCountdown.Core.Models;

namespace RaceCountdown.Core.Feed;

/// <summary>
/// Owns the feed the app is showing. Loads the cached feed or the snapshot bundled with the app (whichever is newer),
/// then refreshes from the network in the background (plan §5.3, D3). Thread-safe; one download at a time.
/// </summary>
public sealed class FeedStore(
    FeedCache cache,
    IEventFeedClient client,
    Func<CancellationToken, Task<Stream>> openBundledSnapshot,
    TimeProvider time)
{
    private readonly SemaphoreSlim gate = new(1, 1);

    // The cached ETag only describes the cached feed; if the bundled snapshot is showing, ask for the full feed.
    private bool feedIsCached;

    /// <summary>The feed to show, or null before <see cref="LoadAsync"/> or if no usable feed exists at all.</summary>
    public EventFeed? Feed { get; private set; }

    public FeedCacheState State { get; private set; } = FeedCacheState.Empty;

    /// <summary>Raised after a refresh attempt, successful or not, so the UI and widgets can redraw.</summary>
    public event EventHandler? Changed;

    /// <summary>Loads the cached feed, falling back to the bundled snapshot. Does not touch the network.</summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            State = await cache.LoadStateAsync(cancellationToken);
            var cached = await cache.LoadFeedAsync(cancellationToken);
            var bundled = await LoadBundledAsync(cancellationToken);

            // An app update can ship a snapshot newer than an old cache, so the newer of the two wins.
            Feed = (cached, bundled) switch
            {
                (null, _) => bundled,
                (_, null) => cached,
                _ => bundled.GeneratedUtc > cached.GeneratedUtc ? bundled : cached,
            };
            feedIsCached = Feed is not null && ReferenceEquals(Feed, cached);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>Downloads the feed. Returns the outcome; failures leave the current feed in place.</summary>
    public async Task<FeedFetchStatus> RefreshAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        FeedFetchStatus status;
        try
        {
            var attempt = time.GetUtcNow();
            var result = await client.FetchAsync(feedIsCached ? State.ETag : null, cancellationToken);
            status = result.Status;

            switch (result.Status)
            {
                case FeedFetchStatus.Updated:
                    // A CDN can briefly serve an older copy; never go backwards.
                    if (Feed is null || result.Feed!.GeneratedUtc >= Feed.GeneratedUtc)
                    {
                        Feed = result.Feed!;
                        await cache.SaveFeedAsync(Feed, cancellationToken);
                        feedIsCached = true;
                    }

                    State = new FeedCacheState(result.ETag, attempt, attempt, LastRefreshFailed: false);
                    break;

                case FeedFetchStatus.NotModified:
                    State = State with { LastSuccessUtc = attempt, LastAttemptUtc = attempt, LastRefreshFailed = false };
                    break;

                default:
                    State = State with { LastAttemptUtc = attempt, LastRefreshFailed = true };
                    break;
            }

            await cache.SaveStateAsync(State, cancellationToken);
        }
        finally
        {
            gate.Release();
        }

        Changed?.Invoke(this, EventArgs.Empty);
        return status;
    }

    private async Task<EventFeed?> LoadBundledAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await openBundledSnapshot(cancellationToken);
            var feed = await FeedSerializer.DeserializeAsync(stream, cancellationToken);
            return FeedValidator.Validate(feed).Count == 0 ? feed : null;
        }
        catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException)
        {
            return null;
        }
    }
}
