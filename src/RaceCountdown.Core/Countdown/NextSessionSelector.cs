using RaceCountdown.Core.Models;

namespace RaceCountdown.Core.Countdown;

public sealed record CountdownTarget(RaceEvent Event, Session Session);

public static class NextSessionSelector
{
    /// <summary>
    /// The earliest matching session in a Confirmed event that has not finished yet
    /// (<c>StartUtc + EstimatedDuration &gt; now</c>), or null when there is none.
    /// </summary>
    public static CountdownTarget? Select(EventFeed feed, EventFilter filter, DateTimeOffset now) =>
        feed.Events
            .Where(e => e.Status == EventStatus.Confirmed && filter.MatchesEvent(e))
            .SelectMany(e => e.Sessions
                .Where(s => filter.MatchesSession(s) && s.EndUtc > now)
                .Select(s => new CountdownTarget(e, s)))
            .OrderBy(t => t.Session.StartUtc)
            .FirstOrDefault();

    /// <summary>
    /// When nothing can be counted down to: the matching event expected next (TBA or provisional),
    /// so the UI can show its dates if they are known. Null when there is none.
    /// </summary>
    public static RaceEvent? SelectUpcomingUnscheduled(EventFeed feed, EventFilter filter, DateTimeOffset now)
    {
        // Compare dates generously (UTC date minus one day) so an event is not dropped early in any time zone.
        var earliestEndDate = DateOnly.FromDateTime(now.UtcDateTime).AddDays(-1);

        return feed.Events
            .Where(e => e.Status is EventStatus.DateTba or EventStatus.Provisional
                        && filter.MatchesEvent(e)
                        && (e.EndDate is null || e.EndDate >= earliestEndDate))
            .OrderBy(e => e.StartDate ?? DateOnly.MaxValue)
            .FirstOrDefault();
    }
}
