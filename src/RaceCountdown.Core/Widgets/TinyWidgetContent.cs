using System.Globalization;
using RaceCountdown.Core.Countdown;

namespace RaceCountdown.Core.Widgets;

/// <summary>How the 1×1 widget shows the countdown.</summary>
public enum TinyCountdownMode
{
    /// <summary>More than 24 hours out: whole days to go, the same count as the other widgets, "12 / DAYS".</summary>
    Days,

    /// <summary>The final 24 hours (race day): hours and minutes, "04:12 / TO GO".</summary>
    HoursMinutes,

    /// <summary>The final 30 minutes: a system-ticked chronometer counting minutes and seconds, "29:45 / TO GO".</summary>
    MinutesSeconds,

    /// <summary>Live or awaiting a schedule: a short word, "LIVE" or "TBA".</summary>
    Message,
}

/// <summary>
/// Ready-to-draw content for the 1×1 widget, which works differently from the other sizes: it shows only the days
/// until race day, switches to hours and minutes for the final 24 hours, and to minutes and seconds for the final
/// <see cref="FinalCountdown"/>.
/// </summary>
/// <param name="Headline">The big text. In <see cref="TinyCountdownMode.MinutesSeconds"/> the widget shows a chronometer instead.</param>
/// <param name="Caption">The small line under the headline.</param>
/// <param name="TargetUtc">The race start, for the chronometer; null unless counting down.</param>
/// <param name="NextRedrawUtc">
/// When the content next changes: the days or minutes dropping, the start of race day or of the final countdown, or
/// the start. Null in <see cref="TinyCountdownMode.Message"/>, when <see cref="WidgetSchedule.NextRedrawUtc"/> covers it.
/// </param>
public sealed record TinyWidgetContent(
    TinyCountdownMode Mode,
    string Headline,
    string Caption,
    string AccessibleText,
    DateTimeOffset? TargetUtc,
    DateTimeOffset? NextRedrawUtc)
{
    /// <summary>How long before the start the 1×1 widget switches to minutes and seconds.</summary>
    public static readonly TimeSpan FinalCountdown = TimeSpan.FromMinutes(30);

    public bool UsesChronometer => Mode == TinyCountdownMode.MinutesSeconds;

    public static TinyWidgetContent Create(WidgetSnapshot snapshot, DateTimeOffset now)
    {
        var inv = CultureInfo.InvariantCulture;

        if (snapshot.Phase is not (CountdownPhase.Counting or CountdownPhase.RaceDay) || snapshot.TargetUtc is not { } start)
        {
            return snapshot.Phase == CountdownPhase.Live
                ? new(TinyCountdownMode.Message, "LIVE", "RACE ON", snapshot.AccessibleText, null, null)
                : new(TinyCountdownMode.Message, "TBA", "NEXT RACE", snapshot.AccessibleText, null, null);
        }

        var remaining = CountdownCalculator.CeilingToSeconds(start - now);

        if (remaining <= FinalCountdown)
        {
            return new(
                TinyCountdownMode.MinutesSeconds,
                string.Format(inv, "{0:00}:{1:00}", (int)remaining.TotalMinutes, remaining.Seconds),
                "TO GO",
                $"The {snapshot.Title} starts in under {FinalCountdown.TotalMinutes:0} minutes",
                start,
                start);
        }

        // Whole days, as on the other widgets: CountdownState.Days while counting, which is more than 24 hours out.
        if (snapshot.Days is { } days)
        {
            return new(
                TinyCountdownMode.Days,
                days.ToString(inv),
                days == 1 ? "DAY" : "DAYS",
                $"{Plural(days, "day")} until the {snapshot.Title}",
                start,
                Earliest(WidgetSchedule.NextDrop(start, now, TimeSpan.FromDays(1)), start - CountdownCalculator.RaceDayWindow));
        }

        var hours = (int)remaining.TotalHours;
        return new(
            TinyCountdownMode.HoursMinutes,
            string.Format(inv, "{0:00}:{1:00}", hours, remaining.Minutes),
            "TO GO",
            $"Race day: {Plural(hours, "hour")}, {Plural(remaining.Minutes, "minute")} until the {snapshot.Title}",
            start,
            Earliest(WidgetSchedule.NextDrop(start, now, TimeSpan.FromMinutes(1)), start - FinalCountdown));
    }

    private static DateTimeOffset Earliest(DateTimeOffset a, DateTimeOffset b) => a < b ? a : b;

    private static string Plural(int count, string unit) => count == 1 ? $"1 {unit}" : $"{count} {unit}s";
}
