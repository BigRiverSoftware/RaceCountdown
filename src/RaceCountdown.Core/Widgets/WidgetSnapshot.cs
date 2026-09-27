using System.Globalization;
using RaceCountdown.Core.Countdown;
using RaceCountdown.Core.Models;

namespace RaceCountdown.Core.Widgets;

/// <summary>
/// Platform-neutral, ready-to-draw widget content (plan §8.1). The Android and Windows widgets render
/// this and contain no countdown logic of their own.
/// </summary>
/// <param name="Title">Event name, e.g. "Repco Bathurst 1000".</param>
/// <param name="Days">Whole days to go while counting; null in the other phases.</param>
/// <param name="Headline">The large text: "12 days", "04:12", "RACE UNDERWAY" or "TBA".</param>
/// <param name="Detail">The smaller line under the headline.</param>
/// <param name="AccessibleText">A full sentence for screen readers.</param>
/// <param name="TargetUtc">The race start, for the Android chronometer; null when awaiting a schedule.</param>
/// <param name="NextPhaseChangeUtc">When the widget must be redrawn because the phase changes.</param>
public sealed record WidgetSnapshot(
    CountdownPhase Phase,
    string Title,
    int? Days,
    string Headline,
    string Detail,
    string AccessibleText,
    DateTimeOffset? TargetUtc,
    DateTimeOffset? NextPhaseChangeUtc,
    bool IsStale)
{
    public static WidgetSnapshot Create(CountdownState state, string fallbackTitle)
    {
        var title = state.Event?.Name ?? fallbackTitle;
        var inv = CultureInfo.InvariantCulture;

        return state.Phase switch
        {
            CountdownPhase.Counting => new WidgetSnapshot(
                state.Phase,
                title,
                state.Days,
                Plural(state.Days, "day"),
                string.Format(inv, "{0:00}h {1:00}m", state.Hours, state.Minutes),
                $"{Plural(state.Days, "day")}, {Plural(state.Hours, "hour")} until the {title}",
                state.Session!.StartUtc,
                state.NextPhaseChangeUtc,
                state.IsStale),

            CountdownPhase.RaceDay => new WidgetSnapshot(
                state.Phase,
                title,
                Days: null,
                string.Format(inv, "{0:00}:{1:00}", (int)state.Remaining.TotalHours, state.Minutes),
                "RACE DAY",
                $"Race day: {Plural((int)state.Remaining.TotalHours, "hour")}, {Plural(state.Minutes, "minute")} until the {title}",
                state.Session!.StartUtc,
                state.NextPhaseChangeUtc,
                state.IsStale),

            CountdownPhase.Live => new WidgetSnapshot(
                state.Phase,
                title,
                Days: null,
                "RACE UNDERWAY",
                string.Format(inv, "Green flag {0}:{1:00} ago", (int)state.Elapsed.TotalHours, state.Elapsed.Minutes),
                $"The {title} is underway",
                state.Session!.StartUtc,
                state.NextPhaseChangeUtc,
                state.IsStale),

            _ => new WidgetSnapshot(
                state.Phase,
                title,
                Days: null,
                "TBA",
                DescribeDates(state.Event),
                $"The next {title} start time has not been announced",
                TargetUtc: null,
                NextPhaseChangeUtc: null,
                state.IsStale),
        };
    }

    private static string DescribeDates(RaceEvent? ev)
    {
        if (ev?.StartDate is not { } start || ev.EndDate is not { } end)
        {
            return "Start time to be announced";
        }

        var inv = CultureInfo.InvariantCulture;
        return start.Month == end.Month
            ? string.Format(inv, "{0:%d}–{1:d MMM yyyy}", start, end)
            : string.Format(inv, "{0:d MMM}–{1:d MMM yyyy}", start, end);
    }

    private static string Plural(int count, string unit) => count == 1 ? $"1 {unit}" : $"{count} {unit}s";
}
