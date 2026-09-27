using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using SizeF = Android.Util.SizeF;
using Android.Views;
using Android.Widget;
using RaceCountdown.Core.Widgets;

namespace RaceCountdown.Widgets;

/// <summary>Turns a <see cref="WidgetSnapshot"/> into the small (2×1) or medium (4×2) widget layout.</summary>
internal static class CountdownWidgetViews
{
    // Below this width (dp) the compact layout is used.
    private const int MediumMinWidthDp = 180;

    public static RemoteViews Build(Context context, WidgetSnapshot snapshot, Bundle? options, DateTimeOffset now)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            // Responsive layouts: the launcher picks the best fit for the widget's current size.
            return new RemoteViews(new Dictionary<SizeF, RemoteViews>
            {
                [new SizeF(40, 40)] = Small(context, snapshot, now),
                [new SizeF(MediumMinWidthDp, 80)] = Medium(context, snapshot, now),
            });
        }

        var minWidth = options?.GetInt(AppWidgetManager.OptionAppwidgetMinWidth) ?? 0;
        return minWidth > 0 && minWidth < MediumMinWidthDp ? Small(context, snapshot, now) : Medium(context, snapshot, now);
    }

    private static RemoteViews Small(Context context, WidgetSnapshot snapshot, DateTimeOffset now)
    {
        var views = new RemoteViews(context.PackageName, Resource.Layout.widget_small);
        SetCountdown(views, snapshot, snapshot.CompactHeadline, now);
        views.SetTextViewText(Resource.Id.widget_detail, snapshot.CompactCaption);
        SetCommon(context, views, snapshot);
        return views;
    }

    private static RemoteViews Medium(Context context, WidgetSnapshot snapshot, DateTimeOffset now)
    {
        var views = new RemoteViews(context.PackageName, Resource.Layout.widget_medium);
        views.SetTextViewText(Resource.Id.widget_title, snapshot.Title);
        SetCountdown(views, snapshot, snapshot.Headline, now);
        views.SetTextViewText(Resource.Id.widget_detail, snapshot.ShortDetail);
        views.SetViewVisibility(Resource.Id.widget_stale, snapshot.IsStale ? ViewStates.Visible : ViewStates.Gone);
        SetCommon(context, views, snapshot);
        return views;
    }

    private static void SetCountdown(RemoteViews views, WidgetSnapshot snapshot, string headline, DateTimeOffset now)
    {
        if (WidgetSchedule.UsesChronometer(snapshot))
        {
            // Chronometer bases are in the elapsed-realtime clock, which is not affected by wall-clock changes.
            var untilStart = snapshot.TargetUtc!.Value - now;
            var baseTime = SystemClock.ElapsedRealtime() + (long)untilStart.TotalMilliseconds;
            views.SetChronometer(Resource.Id.widget_chronometer, baseTime, null, true);
            views.SetChronometerCountDown(Resource.Id.widget_chronometer, true);
            views.SetViewVisibility(Resource.Id.widget_chronometer, ViewStates.Visible);
            views.SetViewVisibility(Resource.Id.widget_headline, ViewStates.Gone);
        }
        else
        {
            views.SetChronometer(Resource.Id.widget_chronometer, 0, null, false);
            views.SetViewVisibility(Resource.Id.widget_chronometer, ViewStates.Gone);
            views.SetViewVisibility(Resource.Id.widget_headline, ViewStates.Visible);
            views.SetTextViewText(Resource.Id.widget_headline, headline);
        }
    }

    private static void SetCommon(Context context, RemoteViews views, WidgetSnapshot snapshot)
    {
        views.SetContentDescription(Resource.Id.widget_root, snapshot.AccessibleText);

        // Tapping opens the app, which refreshes the feed.
        var open = new Intent(context, typeof(MainActivity)).SetFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);
        var pending = PendingIntent.GetActivity(context, 0, open, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        views.SetOnClickPendingIntent(Resource.Id.widget_root, pending);
    }
}
