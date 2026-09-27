using RaceCountdown.Core.Countdown;

namespace RaceCountdown.Core.Widgets;

/// <summary>
/// When a home-screen widget that shows whole hours (the Android widget, plan §8.2) must be redrawn. In the final
/// 24 hours the Android widget shows a chronometer the system ticks by itself, so no redraws are needed until the start.
/// </summary>
public static class WidgetSchedule
{
    /// <summary>While live the text does not change, but check this often so the roll-over is picked up promptly.</summary>
    public static readonly TimeSpan LiveInterval = TimeSpan.FromMinutes(30);

    public static readonly TimeSpan AwaitingInterval = TimeSpan.FromHours(6);

    /// <summary>
    /// The next instant the hour-precision widget text goes out of date: when the hours remaining next drop while
    /// counting down, the start on race day, and periodically while live or waiting for a schedule.
    /// Never later than the next phase change.
    /// </summary>
    public static DateTimeOffset NextRedrawUtc(WidgetSnapshot snapshot, DateTimeOffset now)
    {
        var next = snapshot.Phase switch
        {
            CountdownPhase.Counting => NextHourDrop(snapshot.TargetUtc!.Value, now),
            CountdownPhase.RaceDay => snapshot.TargetUtc!.Value,
            CountdownPhase.Live => now + LiveInterval,
            _ => now + AwaitingInterval,
        };

        return snapshot.NextPhaseChangeUtc is { } change && change > now && change < next ? change : next;
    }

    /// <summary>True when the widget should show a system-ticked countdown (final 24 hours) instead of text.</summary>
    public static bool UsesChronometer(WidgetSnapshot snapshot) => snapshot.Phase == CountdownPhase.RaceDay;

    // The hours shown drop by one just after the time remaining passes a whole number of hours, which is not the
    // wall-clock hour unless the start is on the hour. CountdownCalculator rounds the remaining time up to the
    // second, so at exactly H hours to go it still shows H; redraw one second later.
    private static DateTimeOffset NextHourDrop(DateTimeOffset target, DateTimeOffset now)
    {
        var wholeHours = Math.Floor((target - now).TotalHours);
        return target - TimeSpan.FromHours(wholeHours) + TimeSpan.FromSeconds(1);
    }
}
