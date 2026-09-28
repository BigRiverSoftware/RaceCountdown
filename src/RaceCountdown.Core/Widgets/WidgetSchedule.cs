using RaceCountdown.Core.Countdown;

namespace RaceCountdown.Core.Widgets;

/// <summary>
/// When a home-screen widget must be redrawn. Both widgets show days, hours and minutes while counting. The Android
/// widget (plan §8.2) shows a chronometer the system ticks by itself in the final 24 hours and a fixed line while live,
/// so it uses <see cref="NextRedrawUtc"/>. The Windows widget (plan §8.3) cannot tick, so it uses <see cref="NextMinuteRedrawUtc"/>.
/// </summary>
public static class WidgetSchedule
{
    /// <summary>While live the text does not change, but check this often so the roll-over is picked up promptly.</summary>
    public static readonly TimeSpan LiveInterval = TimeSpan.FromMinutes(30);

    public static readonly TimeSpan AwaitingInterval = TimeSpan.FromHours(6);

    /// <summary>
    /// The next instant the Android widget goes out of date: when the minutes remaining next drop while counting down,
    /// the start on race day, and periodically while live or waiting for a schedule.
    /// Never later than the next phase change.
    /// </summary>
    public static DateTimeOffset NextRedrawUtc(WidgetSnapshot snapshot, DateTimeOffset now)
    {
        var next = snapshot.Phase switch
        {
            CountdownPhase.Counting => NextDrop(snapshot.TargetUtc!.Value, now, TimeSpan.FromMinutes(1)),
            CountdownPhase.RaceDay => snapshot.TargetUtc!.Value,
            CountdownPhase.Live => now + LiveInterval,
            _ => now + AwaitingInterval,
        };

        return NoLaterThanPhaseChange(snapshot, now, next);
    }

    /// <summary>
    /// The next instant minute-precision widget text (the segments, <see cref="WidgetSnapshot.Detail"/> and the
    /// race-day <see cref="WidgetSnapshot.Headline"/>) goes out of date: when the minutes remaining next drop, when the minutes
    /// since the green flag next rise, and periodically while waiting for a schedule. Never later than the next phase change.
    /// </summary>
    public static DateTimeOffset NextMinuteRedrawUtc(WidgetSnapshot snapshot, DateTimeOffset now)
    {
        var next = snapshot.Phase switch
        {
            CountdownPhase.Counting or CountdownPhase.RaceDay => NextDrop(snapshot.TargetUtc!.Value, now, TimeSpan.FromMinutes(1)),
            CountdownPhase.Live => NextRise(snapshot.TargetUtc!.Value, now, TimeSpan.FromMinutes(1)),
            _ => now + AwaitingInterval,
        };

        return NoLaterThanPhaseChange(snapshot, now, next);
    }

    /// <summary>True when the widget should show a system-ticked countdown (final 24 hours) instead of text.</summary>
    public static bool UsesChronometer(WidgetSnapshot snapshot) => snapshot.Phase == CountdownPhase.RaceDay;

    /// <summary>
    /// When to set the Android widget's inexact alarm so that it goes off close to <paramref name="redrawUtc"/>.
    /// Android may deliver an inexact alarm up to 75% of its lead time late (at most an hour), so a redraw more than a
    /// minute ahead is reached in steps of a quarter of the time left. Each step goes off before the redraw is due,
    /// and the last is under a minute ahead, so the redraw is at most about 45 seconds late. The redraws in between
    /// change nothing on screen.
    /// </summary>
    public static DateTimeOffset NextAlarmUtc(DateTimeOffset redrawUtc, DateTimeOffset now)
    {
        var lead = redrawUtc - now;
        if (lead <= TimeSpan.FromMinutes(1))
        {
            return redrawUtc;
        }

        var step = lead / 4;
        return now + (step < MinAlarmStep ? MinAlarmStep : step);
    }

    private static readonly TimeSpan MinAlarmStep = TimeSpan.FromSeconds(30);

    private static DateTimeOffset NoLaterThanPhaseChange(WidgetSnapshot snapshot, DateTimeOffset now, DateTimeOffset next) =>
        snapshot.NextPhaseChangeUtc is { } change && change > now && change < next ? change : next;

    // The units shown drop by one just after the time remaining passes a whole number of units, which is not the
    // wall-clock hour or minute unless the start is on one. CountdownCalculator rounds the remaining time up to the
    // second, so at exactly N units to go it still shows N; redraw one second later.
    internal static DateTimeOffset NextDrop(DateTimeOffset target, DateTimeOffset now, TimeSpan unit)
    {
        var wholeUnits = Math.Floor((target - now) / unit);
        return target - wholeUnits * unit + TimeSpan.FromSeconds(1);
    }

    // The elapsed time is rounded down, so the units shown rise exactly on each whole unit after the start.
    private static DateTimeOffset NextRise(DateTimeOffset start, DateTimeOffset now, TimeSpan unit)
    {
        var wholeUnits = Math.Floor((now - start) / unit);
        return start + (wholeUnits + 1) * unit;
    }
}
