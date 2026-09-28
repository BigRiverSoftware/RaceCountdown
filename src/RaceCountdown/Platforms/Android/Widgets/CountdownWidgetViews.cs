using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using SizeF = Android.Util.SizeF;
using Android.Views;
using Android.Widget;
using RaceCountdown.Core.Widgets;

namespace RaceCountdown.Widgets;

/// <summary>
/// Turns a <see cref="WidgetSnapshot"/> into the small (2×1), medium (4×2) or 2×2 widget layout, and a
/// <see cref="TinyWidgetContent"/> into the 1×1 layout.
/// </summary>
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

    /// <summary>The 2×2 widget (<see cref="SquareCountdownWidgetProvider"/>), which has one layout at every size.</summary>
    public static RemoteViews BuildSquare(Context context, WidgetSnapshot snapshot, DateTimeOffset now)
    {
        var views = new RemoteViews(context.PackageName, Resource.Layout.widget_square);
        SetDetailed(context, views, snapshot, now);
        return views;
    }

    /// <summary>The 1×1 widget (<see cref="TinyCountdownWidgetProvider"/>), which has one layout at every size.</summary>
    public static RemoteViews BuildTiny(Context context, TinyWidgetContent content, DateTimeOffset now)
    {
        var views = new RemoteViews(context.PackageName, Resource.Layout.widget_tiny);
        if (content.UsesChronometer)
        {
            // Under an hour to go, the chronometer shows minutes and seconds.
            ShowChronometer(views, content.TargetUtc!.Value, now);
        }
        else
        {
            ShowHeadline(views, content.Headline);
        }

        views.SetTextViewText(Resource.Id.widget_detail, content.Caption);
        views.SetContentDescription(Resource.Id.widget_root, content.AccessibleText);
        SetOpenApp(context, views);
        return views;
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
        SetDetailed(context, views, snapshot, now);
        return views;
    }

    // The medium and 2×2 layouts: the title, the days/hours/minutes tiles while counting (otherwise the headline or
    // chronometer and the detail line), and the offline line.
    private static void SetDetailed(Context context, RemoteViews views, WidgetSnapshot snapshot, DateTimeOffset now)
    {
        views.SetTextViewText(Resource.Id.widget_title, snapshot.Title);
        SetCountdown(views, snapshot, snapshot.Headline, now);

        views.SetViewVisibility(Resource.Id.widget_segments, snapshot.ShowsSegments ? ViewStates.Visible : ViewStates.Gone);
        views.SetTextViewText(Resource.Id.widget_days, snapshot.DaysText);
        views.SetTextViewText(Resource.Id.widget_hours, snapshot.HoursText);
        views.SetTextViewText(Resource.Id.widget_minutes, snapshot.MinutesText);
        if (snapshot.ShowsSegments)
        {
            views.SetViewVisibility(Resource.Id.widget_headline, ViewStates.Gone);
        }

        // The tiles are labelled, so the detail line is only needed in the other phases.
        views.SetViewVisibility(Resource.Id.widget_detail, snapshot.ShowsSegments ? ViewStates.Gone : ViewStates.Visible);
        views.SetTextViewText(Resource.Id.widget_detail, snapshot.ShortDetail);
        views.SetViewVisibility(Resource.Id.widget_stale, snapshot.IsStale ? ViewStates.Visible : ViewStates.Gone);
        SetCommon(context, views, snapshot);
    }

    private static void SetCountdown(RemoteViews views, WidgetSnapshot snapshot, string headline, DateTimeOffset now)
    {
        if (WidgetSchedule.UsesChronometer(snapshot))
        {
            ShowChronometer(views, snapshot.TargetUtc!.Value, now);
        }
        else
        {
            ShowHeadline(views, headline);
        }
    }

    private static void ShowChronometer(RemoteViews views, DateTimeOffset targetUtc, DateTimeOffset now)
    {
        // Chronometer bases are in the elapsed-realtime clock, which is not affected by wall-clock changes.
        var untilStart = targetUtc - now;
        var baseTime = SystemClock.ElapsedRealtime() + (long)untilStart.TotalMilliseconds;
        views.SetChronometer(Resource.Id.widget_chronometer, baseTime, null, true);
        views.SetChronometerCountDown(Resource.Id.widget_chronometer, true);
        views.SetViewVisibility(Resource.Id.widget_chronometer, ViewStates.Visible);
        views.SetViewVisibility(Resource.Id.widget_headline, ViewStates.Gone);
    }

    private static void ShowHeadline(RemoteViews views, string headline)
    {
        views.SetChronometer(Resource.Id.widget_chronometer, 0, null, false);
        views.SetViewVisibility(Resource.Id.widget_chronometer, ViewStates.Gone);
        views.SetViewVisibility(Resource.Id.widget_headline, ViewStates.Visible);
        views.SetTextViewText(Resource.Id.widget_headline, headline);
    }

    private static void SetCommon(Context context, RemoteViews views, WidgetSnapshot snapshot)
    {
        views.SetContentDescription(Resource.Id.widget_root, snapshot.AccessibleText);
        SetOpenApp(context, views);
    }

    private static void SetOpenApp(Context context, RemoteViews views)
    {
        // Tapping opens the app, which refreshes the feed.
        var open = new Intent(context, typeof(MainActivity)).SetFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);
        var pending = PendingIntent.GetActivity(context, 0, open, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        views.SetOnClickPendingIntent(Resource.Id.widget_root, pending);
    }
}
