using RaceCountdown.Core.Models;

namespace RaceCountdown.Core.Countdown;

public enum CountdownPhase
{
    /// <summary>More than 24 hours before the start.</summary>
    Counting,

    /// <summary>The final 24 hours before the start.</summary>
    RaceDay,

    /// <summary>Between the start and the estimated finish.</summary>
    Live,

    /// <summary>No future race has a published start time; show "TBA" (plan D13).</summary>
    AwaitingSchedule,
}

/// <summary>
/// Everything the page and the widgets need to draw a countdown at one instant.
/// <para>
/// Plan §6 lists "Stale" as a state, but the countdown is still shown when the feed is stale,
/// so it is the <see cref="IsStale"/> flag on top of the other phases.
/// </para>
/// </summary>
/// <param name="Event">The event being counted down to, or for <see cref="CountdownPhase.AwaitingSchedule"/> the TBA event if one is known.</param>
/// <param name="Remaining">Time to the start, rounded up to whole seconds. Zero once live or when awaiting a schedule.</param>
/// <param name="Elapsed">Time since the start, rounded down to whole seconds. Zero unless live.</param>
public sealed record CountdownState(
    CountdownPhase Phase,
    RaceEvent? Event,
    Session? Session,
    Track? Track,
    TimeSpan Remaining,
    TimeSpan Elapsed,
    bool IsStale)
{
    public int Days => Remaining.Days;

    public int Hours => Remaining.Hours;

    public int Minutes => Remaining.Minutes;

    public int Seconds => Remaining.Seconds;

    /// <summary>
    /// When the phase next changes (start − 24h, start, estimated finish), so widgets can schedule a redraw.
    /// Null when awaiting a schedule.
    /// </summary>
    public DateTimeOffset? NextPhaseChangeUtc => Phase switch
    {
        CountdownPhase.Counting => Session!.StartUtc - CountdownCalculator.RaceDayWindow,
        CountdownPhase.RaceDay => Session!.StartUtc,
        CountdownPhase.Live => Session!.EndUtc,
        _ => null,
    };
}
