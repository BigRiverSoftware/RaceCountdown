using RaceCountdown.Core.Countdown;
using RaceCountdown.Core.Feed;
using RaceCountdown.Core.Models;
using RaceCountdown.Core.Widgets;
using RaceCountdown.Services;

namespace RaceCountdown.Widgets;

/// <summary>
/// The feed as seen from the widget provider, which runs in its own process without the MAUI UI. Uses the same
/// cache folder (the package's local app data) and bundled snapshot as the app, so a widget pinned before the app
/// is first opened still shows the race.
/// </summary>
internal static class WindowsFeed
{
    private static readonly Lazy<HttpClient> Http = new(FeedHttp.Create);

    public static FeedStore CreateStore() => new(
        new FeedCache(FileSystem.Current.AppDataDirectory),
        new EventFeedClient(Http.Value, EventFeedClient.DefaultFeedUrl),
        _ => FileSystem.Current.OpenAppPackageFileAsync(MauiProgram.BundledSnapshotName),
        TimeProvider.System);

    public static WidgetSnapshot Snapshot(FeedStore store, DateTimeOffset now)
    {
        var state = CountdownCalculator.Calculate(
            store.Feed ?? EventFeed.Empty, EventFilter.Bathurst1000MainRace, now, store.State.LastRefreshFailed);
        return WidgetSnapshot.Create(state, CountdownDisplay.DefaultTitle);
    }
}
