using RaceCountdown.Core.Countdown;
using RaceCountdown.Core.Models;

namespace RaceCountdown.Core.Feed;

/// <summary>
/// When to download the feed again (plan §5.3): every 12 hours, hourly in race week, and straight after a followed
/// race starts or finishes so the next race is picked up. Failed downloads are retried no more often than
/// <see cref="RetryAfterFailure"/>. The app's first refresh at start-up is forced and ignores this policy.
/// </summary>
public static class RefreshPolicy
{
    public static readonly TimeSpan NormalInterval = TimeSpan.FromHours(12);
    public static readonly TimeSpan RaceWeekInterval = TimeSpan.FromHours(1);
    public static readonly TimeSpan RaceWeek = TimeSpan.FromDays(7);
    public static readonly TimeSpan RetryAfterFailure = TimeSpan.FromMinutes(15);

    public static bool IsDue(EventFeed? feed, EventFilter filter, FeedCacheState state, DateTimeOffset now)
    {
        if (state.LastAttemptUtc is { } attempt && now - attempt < RetryAfterFailure)
        {
            return false;
        }

        if (state.LastSuccessUtc is not { } success)
        {
            return true;
        }

        return now - success >= Interval(feed, filter, now)
               || (feed is not null && RaceStartedOrFinishedSince(feed, filter, success, now));
    }

    /// <summary>Hourly when the next followed race starts within a week or is under way, otherwise 12-hourly.</summary>
    public static TimeSpan Interval(EventFeed? feed, EventFilter filter, DateTimeOffset now) =>
        feed is not null && NextSessionSelector.Select(feed, filter, now) is { } target && target.Session.StartUtc - now <= RaceWeek
            ? RaceWeekInterval
            : NormalInterval;

    private static bool RaceStartedOrFinishedSince(EventFeed feed, EventFilter filter, DateTimeOffset since, DateTimeOffset now) =>
        feed.Events
            .Where(e => e.Status == EventStatus.Confirmed && filter.MatchesEvent(e))
            .SelectMany(e => e.Sessions)
            .Where(filter.MatchesSession)
            .Any(s => (s.StartUtc > since && s.StartUtc <= now) || (s.EndUtc > since && s.EndUtc <= now));
}
