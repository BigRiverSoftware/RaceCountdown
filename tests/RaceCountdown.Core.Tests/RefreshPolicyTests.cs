using RaceCountdown.Core.Countdown;
using RaceCountdown.Core.Feed;
using RaceCountdown.Core.Models;

namespace RaceCountdown.Core.Tests;

public class RefreshPolicyTests
{
    private static readonly DateTimeOffset RaceStart = TestFeeds.RaceStart2026;
    private static readonly EventFeed Feed = TestFeeds.Feed(TestFeeds.Bathurst2026());
    private static readonly EventFilter Filter = EventFilter.Bathurst1000MainRace;

    private static FeedCacheState SucceededAt(DateTimeOffset at) => new("\"v1\"", at, at);

    private static bool IsDue(FeedCacheState state, DateTimeOffset now, EventFeed? feed = null) =>
        RefreshPolicy.IsDue(feed ?? Feed, Filter, state, now);

    [Fact]
    public void Never_refreshed_is_due()
    {
        Assert.True(IsDue(FeedCacheState.Empty, RaceStart.AddDays(-60)));
    }

    [Fact]
    public void Due_every_12_hours_outside_race_week()
    {
        var now = RaceStart.AddDays(-30);

        Assert.False(IsDue(SucceededAt(now.AddHours(-11.9)), now));
        Assert.True(IsDue(SucceededAt(now.AddHours(-12)), now));
    }

    [Fact]
    public void Due_hourly_in_race_week()
    {
        var now = RaceStart.AddDays(-6);

        Assert.False(IsDue(SucceededAt(now.AddMinutes(-59)), now));
        Assert.True(IsDue(SucceededAt(now.AddHours(-1)), now));
    }

    [Fact]
    public void Due_hourly_while_the_race_is_live()
    {
        Assert.Equal(RefreshPolicy.RaceWeekInterval, RefreshPolicy.Interval(Feed, Filter, RaceStart.AddHours(1)));
    }

    [Fact]
    public void Twelve_hourly_with_no_feed_or_no_upcoming_race()
    {
        Assert.Equal(RefreshPolicy.NormalInterval, RefreshPolicy.Interval(null, Filter, RaceStart.AddDays(-1)));
        Assert.Equal(RefreshPolicy.NormalInterval, RefreshPolicy.Interval(Feed, Filter, RaceStart.AddDays(1)));
    }

    [Fact]
    public void Due_straight_after_the_race_starts()
    {
        var lastSuccess = RaceStart.AddMinutes(-30);

        Assert.False(IsDue(SucceededAt(lastSuccess), RaceStart.AddMinutes(-1)));
        Assert.True(IsDue(SucceededAt(lastSuccess), RaceStart.AddMinutes(1)));
    }

    [Fact]
    public void Due_straight_after_the_race_finishes()
    {
        var end = RaceStart + TimeSpan.FromHours(7);
        var lastSuccess = end.AddMinutes(-30);

        Assert.False(IsDue(SucceededAt(lastSuccess), end.AddMinutes(-1)));
        Assert.True(IsDue(SucceededAt(lastSuccess), end.AddMinutes(1)));
    }

    [Fact]
    public void Failed_attempts_are_retried_after_15_minutes()
    {
        var now = RaceStart.AddDays(-30);
        var failed = new FeedCacheState("\"v1\"", now.AddDays(-2), now.AddMinutes(-14), LastRefreshFailed: true);

        Assert.False(IsDue(failed, now));
        Assert.True(IsDue(failed with { LastAttemptUtc = now.AddMinutes(-15) }, now));
    }

    [Fact]
    public void Ignores_other_events()
    {
        var sandown = new RaceEvent(
            "supercars-2026-sandown", "supercars", "mount-panorama", "Sandown 500",
            new DateOnly(2026, 9, 10), new DateOnly(2026, 9, 12), EventStatus.Confirmed,
            [TestFeeds.Race(new DateTimeOffset(2026, 9, 12, 3, 0, 0, TimeSpan.Zero))]);
        var feed = TestFeeds.Feed(TestFeeds.Bathurst2026(), sandown);
        var lastSuccess = new DateTimeOffset(2026, 9, 12, 2, 0, 0, TimeSpan.Zero);

        Assert.False(IsDue(SucceededAt(lastSuccess), lastSuccess.AddHours(2), feed));
    }

    [Fact]
    public void No_feed_uses_the_normal_interval()
    {
        var now = RaceStart.AddDays(-30);

        Assert.False(RefreshPolicy.IsDue(null, Filter, SucceededAt(now.AddHours(-1)), now));
        Assert.True(RefreshPolicy.IsDue(null, Filter, SucceededAt(now.AddHours(-12)), now));
    }
}
