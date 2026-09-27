using Android.Content;
using Android.Runtime;
using Android.Util;
using AndroidX.Work;
using RaceCountdown.Widgets;

namespace RaceCountdown.Work;

/// <summary>
/// Downloads the feed in the background (plan §8.2): every 12 hours while a widget exists, and on demand when the
/// widget's redraw finds <see cref="Core.Feed.RefreshPolicy"/> due. Only runs with a network connection.
/// </summary>
[Register("au.bigriversoftware.bathurstcountdown.FeedRefreshWorker")]
public class FeedRefreshWorker(Context context, WorkerParameters workerParams) : Worker(context, workerParams)
{
    private const string Tag = "FeedRefreshWorker";
    private const string PeriodicName = "feed-refresh";
    private const string OneOffName = "feed-refresh-now";

    private readonly Context appContext = context.ApplicationContext ?? context;

    public override Result DoWork()
    {
        try
        {
            var store = AndroidFeed.CreateStore(appContext);
            store.LoadAsync().GetAwaiter().GetResult();
            var status = store.RefreshAsync().GetAwaiter().GetResult();
            Log.Info(Tag, $"Feed refresh: {status}");
        }
        catch (Exception ex)
        {
            Log.Error(Tag, $"Feed refresh failed: {ex}");
        }

        // Redraw even on failure so a "stale" flag appears when it should. Failures are not retried here: the
        // widget's redraws retry through RefreshPolicy, which limits attempts to one per 15 minutes.
        CountdownWidgetProvider.UpdateAll(appContext);
        return Result.InvokeSuccess()!;
    }

    public static void EnsureScheduled(Context context)
    {
        var request = PeriodicWorkRequest.Builder.From<FeedRefreshWorker>(TimeSpan.FromHours(12))
            .SetConstraints(NetworkConstraint())
            .Build();
        WorkManager.GetInstance(context).EnqueueUniquePeriodicWork(PeriodicName, ExistingPeriodicWorkPolicy.Keep!, (PeriodicWorkRequest)request);
    }

    public static void RefreshNow(Context context)
    {
        var request = OneTimeWorkRequest.Builder.From<FeedRefreshWorker>()
            .SetConstraints(NetworkConstraint())
            .Build();
        WorkManager.GetInstance(context).EnqueueUniqueWork(OneOffName, ExistingWorkPolicy.Keep!, (OneTimeWorkRequest)request);
    }

    public static void Cancel(Context context)
    {
        var work = WorkManager.GetInstance(context);
        work.CancelUniqueWork(PeriodicName);
        work.CancelUniqueWork(OneOffName);
    }

    private static Constraints NetworkConstraint() =>
        new Constraints.Builder().SetRequiredNetworkType(NetworkType.Connected!).Build();
}
