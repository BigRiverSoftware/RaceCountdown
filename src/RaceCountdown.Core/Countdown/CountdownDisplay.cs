using System.Globalization;
using RaceCountdown.Core.Feed;
using RaceCountdown.Core.Models;
using RaceCountdown.Core.Widgets;

namespace RaceCountdown.Core.Countdown;

/// <summary>
/// Ready-to-bind text for the countdown page (plan §7), built once per tick. Keeps formatting out of the view model
/// so it can be unit-tested.
/// </summary>
/// <param name="Subtitle">Series and circuit, e.g. "Repco Supercars Championship · Mount Panorama Circuit, Bathurst, NSW".</param>
/// <param name="ShowCountdown">True while counting down (the segmented days/hours/minutes/seconds block is shown).</param>
/// <param name="ShowDays">False on race day, when the days segment would always be zero.</param>
/// <param name="Banner">"RACE DAY", "RACE UNDERWAY" or "TBA"; null while counting.</param>
/// <param name="Detail">Elapsed time while live, or the event dates while awaiting a schedule.</param>
/// <param name="StartTime">Start in track and user time; null when there is no start time.</param>
/// <param name="Footer">Where the data came from and when it was last updated.</param>
public sealed record CountdownDisplay(
    CountdownPhase Phase,
    string Title,
    string Subtitle,
    bool ShowCountdown,
    bool ShowDays,
    string Days,
    string Hours,
    string Minutes,
    string Seconds,
    string? Banner,
    string? Detail,
    string? StartTime,
    string AccessibleText,
    string Footer,
    bool IsStale)
{
    public const string DefaultTitle = "Bathurst 1000";

    public bool HasBanner => Banner is not null;

    public bool HasDetail => Detail is not null;

    public bool HasStartTime => StartTime is not null;

    public static CountdownDisplay Create(
        CountdownState state,
        EventFeed? feed,
        FeedCacheState cacheState,
        TimeZoneInfo userZone,
        CultureInfo culture)
    {
        var title = state.Event?.Name ?? DefaultTitle;
        var subtitle = DescribeSubtitle(state, feed);
        var footer = DescribeFooter(state, feed, cacheState, userZone, culture);
        var startTime = state.Session is { } session && state.Track is { } track
            ? StartTimeFormatter.Format(session, TrackTime.FindZone(track.TimeZoneId), userZone, culture)
            : null;
        var inv = CultureInfo.InvariantCulture;
        var totalHours = (int)state.Remaining.TotalHours;

        return state.Phase switch
        {
            CountdownPhase.Counting => new CountdownDisplay(
                state.Phase, title, subtitle, ShowCountdown: true, ShowDays: true,
                state.Days.ToString(inv), state.Hours.ToString("00", inv), state.Minutes.ToString("00", inv), state.Seconds.ToString("00", inv),
                Banner: null, Detail: null, startTime,
                $"{Plural(state.Days, "day")}, {Plural(state.Hours, "hour")}, {Plural(state.Minutes, "minute")} until the {title}",
                footer, state.IsStale),

            CountdownPhase.RaceDay => new CountdownDisplay(
                state.Phase, title, subtitle, ShowCountdown: true, ShowDays: false,
                "0", totalHours.ToString("00", inv), state.Minutes.ToString("00", inv), state.Seconds.ToString("00", inv),
                "RACE DAY", Detail: null, startTime,
                $"Race day: {Plural(totalHours, "hour")}, {Plural(state.Minutes, "minute")} until the {title}",
                footer, state.IsStale),

            CountdownPhase.Live => new CountdownDisplay(
                state.Phase, title, subtitle, ShowCountdown: false, ShowDays: false,
                "0", "00", "00", "00",
                "RACE UNDERWAY",
                string.Format(inv, "Green flag {0}:{1:00}:{2:00} ago", (int)state.Elapsed.TotalHours, state.Elapsed.Minutes, state.Elapsed.Seconds),
                startTime,
                $"The {title} is underway",
                footer, state.IsStale),

            _ => new CountdownDisplay(
                state.Phase, title, subtitle, ShowCountdown: false, ShowDays: false,
                "0", "00", "00", "00",
                "TBA",
                WidgetSnapshot.DescribeDates(state.Event),
                StartTime: null,
                $"The next {title} start time has not been announced",
                footer, state.IsStale),
        };
    }

    private static string DescribeSubtitle(CountdownState state, EventFeed? feed)
    {
        var series = state.Event is { } ev ? feed?.FindSeries(ev.SeriesId)?.Name : null;
        var track = state.Track is { } t ? $"{t.Name}, {t.Location}" : null;
        return string.Join(" · ", new[] { series, track }.Where(s => s is not null));
    }

    private static string DescribeFooter(CountdownState state, EventFeed? feed, FeedCacheState cacheState, TimeZoneInfo userZone, CultureInfo culture)
    {
        if (feed is null)
        {
            return "No race data yet. Check your connection.";
        }

        string When(DateTimeOffset instant)
        {
            var local = TimeZoneInfo.ConvertTime(instant, userZone);
            return $"{local.ToString("d MMM", culture)}, {local.ToString("t", culture)}";
        }

        if (state.IsStale)
        {
            return $"Offline — race data from {When(feed.GeneratedUtc)}";
        }

        return cacheState.LastSuccessUtc is { } success
            ? $"Updated {When(success)}"
            : $"Using built-in race data from {When(feed.GeneratedUtc)}";
    }

    private static string Plural(int count, string unit) => count == 1 ? $"1 {unit}" : $"{count} {unit}s";
}
