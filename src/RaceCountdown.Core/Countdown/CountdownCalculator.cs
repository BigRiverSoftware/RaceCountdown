using RaceCountdown.Core.Models;

namespace RaceCountdown.Core.Countdown;

public static class CountdownCalculator
{
    public static readonly TimeSpan RaceDayWindow = TimeSpan.FromHours(24);

    /// <summary>A feed older than this is stale when the latest refresh also failed.</summary>
    public static readonly TimeSpan StaleAfter = TimeSpan.FromDays(14);

    public static CountdownState Calculate(EventFeed feed, EventFilter filter, TimeProvider time, bool lastRefreshFailed = false) =>
        Calculate(feed, filter, time.GetUtcNow(), lastRefreshFailed);

    public static CountdownState Calculate(EventFeed feed, EventFilter filter, DateTimeOffset now, bool lastRefreshFailed = false)
    {
        var isStale = lastRefreshFailed && now - feed.GeneratedUtc > StaleAfter;
        var target = NextSessionSelector.Select(feed, filter, now);

        if (target is null)
        {
            var tba = NextSessionSelector.SelectUpcomingUnscheduled(feed, filter, now);
            return new CountdownState(
                CountdownPhase.AwaitingSchedule,
                tba,
                Session: null,
                tba is null ? null : feed.FindTrack(tba.TrackId),
                TimeSpan.Zero,
                TimeSpan.Zero,
                isStale);
        }

        var (ev, session) = target;
        var track = feed.FindTrack(ev.TrackId);
        var untilStart = session.StartUtc - now;

        if (untilStart <= TimeSpan.Zero)
        {
            return new CountdownState(CountdownPhase.Live, ev, session, track, TimeSpan.Zero, FloorToSeconds(-untilStart), isStale);
        }

        var phase = untilStart <= RaceDayWindow ? CountdownPhase.RaceDay : CountdownPhase.Counting;
        return new CountdownState(phase, ev, session, track, CeilingToSeconds(untilStart), TimeSpan.Zero, isStale);
    }

    // Rounding up means the display reaches 00:00:00 exactly at the start, not a second early.
    private static TimeSpan CeilingToSeconds(TimeSpan value) =>
        TimeSpan.FromSeconds(Math.Ceiling(value.Ticks / (double)TimeSpan.TicksPerSecond));

    private static TimeSpan FloorToSeconds(TimeSpan value) =>
        TimeSpan.FromTicks(value.Ticks - (value.Ticks % TimeSpan.TicksPerSecond));
}
