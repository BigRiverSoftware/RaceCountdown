using System.Net.Http.Headers;
using Android.Content;
using RaceCountdown.Core.Feed;

namespace RaceCountdown.Widgets;

/// <summary>
/// The feed as seen from the widget and the background worker, which run without the MAUI UI. Uses the same cache
/// folder as the app (<c>FileSystem.AppDataDirectory</c> is <c>Context.FilesDir</c> on Android) and the same bundled
/// snapshot, so a widget added before the app is first opened still shows the race.
/// </summary>
internal static class AndroidFeed
{
    private static readonly Lazy<HttpClient> Http = new(() =>
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("BathurstCountdown", AppInfo.Current.VersionString));
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(+https://bigriversoftware.au)"));
        return http;
    });

    public static FeedStore CreateStore(Context context) => new(
        new FeedCache(context.FilesDir!.AbsolutePath),
        new EventFeedClient(Http.Value, EventFeedClient.DefaultFeedUrl),
        _ => Task.FromResult(context.Assets!.Open(MauiProgram.BundledSnapshotName)),
        TimeProvider.System);
}
