using Android.Content;
using RaceCountdown.Core.Feed;
using RaceCountdown.Services;

namespace RaceCountdown.Widgets;

/// <summary>
/// The feed as seen from the widget and the background worker, which run without the MAUI UI. Uses the same cache
/// folder as the app (<c>FileSystem.AppDataDirectory</c> is <c>Context.FilesDir</c> on Android) and the same bundled
/// snapshot, so a widget added before the app is first opened still shows the race.
/// </summary>
internal static class AndroidFeed
{
    private static readonly Lazy<HttpClient> Http = new(FeedHttp.Create);

    public static FeedStore CreateStore(Context context) => new(
        new FeedCache(context.FilesDir!.AbsolutePath),
        new EventFeedClient(Http.Value, EventFeedClient.DefaultFeedUrl),
        _ => Task.FromResult(context.Assets!.Open(MauiProgram.BundledSnapshotName)),
        TimeProvider.System);
}
