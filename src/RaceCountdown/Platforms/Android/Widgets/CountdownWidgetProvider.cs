using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Util;
using RaceCountdown.Core.Countdown;
using RaceCountdown.Core.Feed;
using RaceCountdown.Core.Models;
using RaceCountdown.Core.Widgets;
using RaceCountdown.Work;

namespace RaceCountdown.Widgets;

/// <summary>
/// The home-screen countdown widget (plan §8.2). Draws <see cref="WidgetSnapshot"/> and holds no countdown logic of
/// its own. More than 24 hours out it shows days, hours and minutes and redraws on an inexact alarm; in the final
/// 24 hours a chronometer ticks with no wake-ups. All widget instances, including the 2×2
/// <see cref="SquareCountdownWidgetProvider"/> and the 1×1 <see cref="TinyCountdownWidgetProvider"/> (which has its
/// own stages, <see cref="TinyWidgetContent"/>), show the same race, so one alarm serves them all.
/// </summary>
[BroadcastReceiver(Name = "au.bigriversoftware.bathurstcountdown.CountdownWidgetProvider", Label = "Bathurst Countdown", Exported = true)]
[IntentFilter([AppWidgetManager.ActionAppwidgetUpdate])]
[MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/countdown_widget_info")]
public class CountdownWidgetProvider : AppWidgetProvider
{
    private const string Tag = "CountdownWidget";
    private const string ActionRedraw = "au.bigriversoftware.bathurstcountdown.action.REDRAW_WIDGETS";

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is not null && intent?.Action == ActionRedraw)
        {
            RedrawInBackground(context, GoAsync());
            return;
        }

        base.OnReceive(context, intent);
    }

    public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
    {
        if (context is not null)
        {
            RedrawInBackground(context, GoAsync());
        }
    }

    public override void OnAppWidgetOptionsChanged(Context? context, AppWidgetManager? appWidgetManager, int appWidgetId, Android.OS.Bundle? newOptions)
    {
        // Resized: below API 31 the layout is picked from the size, so redraw.
        if (context is not null)
        {
            RedrawInBackground(context, GoAsync());
        }
    }

    public override void OnDisabled(Context? context)
    {
        // The last widget of this kind was removed: once no widget of any kind is left, stop the alarm and the background refresh.
        if (context is not null && !HasWidgets(context))
        {
            AlarmManager(context).Cancel(RedrawIntent(context));
            FeedRefreshWorker.Cancel(context);
        }

        base.OnDisabled(context);
    }

    /// <summary>Redraws every widget from the cached feed. Safe to call from anywhere; does nothing when no widget exists.</summary>
    public static void UpdateAll(Context context) => RedrawInBackground(context, pending: null);

    private static void RedrawInBackground(Context context, PendingResult? pending)
    {
        var appContext = context.ApplicationContext ?? context;
        Task.Run(async () =>
        {
            try
            {
                await RedrawAsync(appContext);
            }
            catch (Exception ex)
            {
                Log.Error(Tag, $"Widget redraw failed: {ex}");
            }
            finally
            {
                pending?.Finish();
            }
        });
    }

    private static async Task RedrawAsync(Context context)
    {
        var manager = AppWidgetManager.GetInstance(context)!;
        var ids = WidgetIds(context, manager, typeof(CountdownWidgetProvider));
        var squareIds = WidgetIds(context, manager, typeof(SquareCountdownWidgetProvider));
        var tinyIds = WidgetIds(context, manager, typeof(TinyCountdownWidgetProvider));
        if (ids.Length == 0 && squareIds.Length == 0 && tinyIds.Length == 0)
        {
            return;
        }

        var store = AndroidFeed.CreateStore(context);
        await store.LoadAsync();

        var filter = EventFilter.Bathurst1000MainRace;
        var now = DateTimeOffset.UtcNow;
        var state = CountdownCalculator.Calculate(store.Feed ?? EventFeed.Empty, filter, now, store.State.LastRefreshFailed);
        var snapshot = WidgetSnapshot.Create(state, CountdownDisplay.DefaultTitle);

        foreach (var id in ids)
        {
            manager.UpdateAppWidget(id, CountdownWidgetViews.Build(context, snapshot, manager.GetAppWidgetOptions(id), now));
        }

        foreach (var id in squareIds)
        {
            manager.UpdateAppWidget(id, CountdownWidgetViews.BuildSquare(context, snapshot, now));
        }

        var nextRedraw = WidgetSchedule.NextRedrawUtc(snapshot, now);
        if (tinyIds.Length > 0)
        {
            var tiny = TinyWidgetContent.Create(snapshot, now);
            foreach (var id in tinyIds)
            {
                manager.UpdateAppWidget(id, CountdownWidgetViews.BuildTiny(context, tiny, now));
            }

            if (tiny.NextRedrawUtc is { } tinyRedraw && tinyRedraw < nextRedraw)
            {
                nextRedraw = tinyRedraw;
            }
        }

        ScheduleRedraw(context, WidgetSchedule.NextAlarmUtc(nextRedraw, now));
        FeedRefreshWorker.EnsureScheduled(context);

        // Hourly in race week, straight after the start, and so on (plan §5.3): the alarm-driven redraws are
        // frequent enough to drive the refresh policy; the 12-hour worker covers the rest.
        if (RefreshPolicy.IsDue(store.Feed, filter, store.State, now))
        {
            FeedRefreshWorker.RefreshNow(context);
        }
    }

    private static int[] WidgetIds(Context context, AppWidgetManager manager, Type provider) =>
        manager.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(provider))) ?? [];

    private static bool HasWidgets(Context context)
    {
        var manager = AppWidgetManager.GetInstance(context)!;
        return WidgetIds(context, manager, typeof(CountdownWidgetProvider)).Length > 0
            || WidgetIds(context, manager, typeof(SquareCountdownWidgetProvider)).Length > 0
            || WidgetIds(context, manager, typeof(TinyCountdownWidgetProvider)).Length > 0;
    }

    private static void ScheduleRedraw(Context context, DateTimeOffset whenUtc)
    {
        // Inexact and non-waking: the widget is only seen when the screen is on, and no exact-alarm permission is
        // needed. WidgetSchedule.NextAlarmUtc steps towards far-off redraws so the inexact delay stays small.
        AlarmManager(context).Set(AlarmType.Rtc, whenUtc.ToUnixTimeMilliseconds(), RedrawIntent(context));
        Log.Debug(Tag, $"Next widget redraw at {whenUtc:O}");
    }

    private static PendingIntent RedrawIntent(Context context)
    {
        var intent = new Intent(context, typeof(CountdownWidgetProvider)).SetAction(ActionRedraw);
        return PendingIntent.GetBroadcast(context, 0, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
    }

    private static AlarmManager AlarmManager(Context context) =>
        (AlarmManager)context.GetSystemService(Context.AlarmService)!;
}
