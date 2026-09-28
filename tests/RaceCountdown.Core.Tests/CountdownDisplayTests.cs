using System.Globalization;
using RaceCountdown.Core.Countdown;
using RaceCountdown.Core.Feed;
using RaceCountdown.Core.Models;

namespace RaceCountdown.Core.Tests;

public class CountdownDisplayTests
{
    private static readonly DateTimeOffset RaceStart = TestFeeds.RaceStart2026;
    private static readonly TimeZoneInfo Sydney = TrackTime.FindZone("Australia/Sydney");
    private static readonly TimeZoneInfo Perth = TrackTime.FindZone("Australia/Perth");
    private static readonly CultureInfo AuCulture = CultureInfo.GetCultureInfo("en-AU");
    private static readonly FeedCacheState Updated = new("\"v1\"", new DateTimeOffset(2026, 9, 27, 23, 15, 0, TimeSpan.Zero));

    private static CountdownDisplay DisplayAt(
        DateTimeOffset now,
        EventFeed? feed = null,
        FeedCacheState? cacheState = null,
        TimeZoneInfo? userZone = null)
    {
        feed ??= TestFeeds.Feed(TestFeeds.Bathurst2026());
        cacheState ??= Updated;
        var state = CountdownCalculator.Calculate(feed, EventFilter.Bathurst1000MainRace, now, cacheState.LastRefreshFailed);
        return CountdownDisplay.Create(state, feed, cacheState, userZone ?? Sydney, AuCulture);
    }

    [Fact]
    public void Counting()
    {
        var display = DisplayAt(RaceStart - new TimeSpan(13, 4, 7, 30));

        Assert.Equal(CountdownPhase.Counting, display.Phase);
        Assert.Equal("Bathurst 1000", display.Title);
        Assert.Equal("Supercars Championship · Mount Panorama Circuit, Bathurst, NSW", display.Subtitle);
        Assert.True(display.ShowCountdown);
        Assert.True(display.ShowDays);
        Assert.Equal(("13", "04", "07", "30"), (display.Days, display.Hours, display.Minutes, display.Seconds));
        Assert.Null(display.Banner);
        Assert.False(display.HasBanner);
        Assert.Null(display.Detail);
        Assert.False(display.HasDetail);
        Assert.True(display.HasStartTime);
        Assert.Equal("Sun 11 Oct, 11:30 am AEDT", display.StartTime);
        Assert.Equal("13 days, 4 hours, 7 minutes until the Bathurst 1000", display.AccessibleText);
        Assert.Equal("Updated 28 Sept, 9:15 am", display.Footer); // AEST: DST starts 4 Oct
        Assert.False(display.IsStale);
    }

    [Fact]
    public void Counting_shows_the_users_own_time_too()
    {
        var display = DisplayAt(RaceStart.AddDays(-2), userZone: Perth);

        Assert.Equal("Sun 11 Oct, 11:30 am AEDT · 8:30 am AWST (your time)", display.StartTime);
    }

    [Fact]
    public void Singular_units()
    {
        Assert.Equal(
            "1 day, 1 hour, 1 minute until the Bathurst 1000",
            DisplayAt(RaceStart - new TimeSpan(1, 1, 1, 0)).AccessibleText);
    }

    [Fact]
    public void Race_day_hides_the_days_segment()
    {
        var display = DisplayAt(RaceStart - new TimeSpan(4, 12, 9));

        Assert.Equal(CountdownPhase.RaceDay, display.Phase);
        Assert.True(display.ShowCountdown);
        Assert.False(display.ShowDays);
        Assert.Equal(("04", "12", "09"), (display.Hours, display.Minutes, display.Seconds));
        Assert.Equal("RACE DAY", display.Banner);
        Assert.Equal("Race day: 4 hours, 12 minutes until the Bathurst 1000", display.AccessibleText);
    }

    [Fact]
    public void Race_day_at_exactly_24_hours_shows_24()
    {
        Assert.Equal("24", DisplayAt(RaceStart.AddHours(-24)).Hours);
    }

    [Fact]
    public void Live()
    {
        var display = DisplayAt(RaceStart + new TimeSpan(1, 23, 45));

        Assert.Equal(CountdownPhase.Live, display.Phase);
        Assert.False(display.ShowCountdown);
        Assert.Equal("RACE UNDERWAY", display.Banner);
        Assert.Equal("Green flag 1:23:45 ago", display.Detail);
        Assert.NotNull(display.StartTime);
        Assert.Equal("The Bathurst 1000 is underway", display.AccessibleText);
    }

    [Fact]
    public void Awaiting_schedule_with_dates()
    {
        var feed = TestFeeds.Feed(TestFeeds.Tba(2027, new DateOnly(2027, 10, 7), new DateOnly(2027, 10, 10)));

        var display = DisplayAt(RaceStart.AddDays(10), feed);

        Assert.Equal(CountdownPhase.AwaitingSchedule, display.Phase);
        Assert.Equal("Bathurst 1000", display.Title);
        Assert.False(display.ShowCountdown);
        Assert.Equal("TBA", display.Banner);
        Assert.Equal("7–10 Oct 2027", display.Detail);
        Assert.True(display.HasBanner && display.HasDetail);
        Assert.Null(display.StartTime);
        Assert.False(display.HasStartTime);
        Assert.Equal("The next Bathurst 1000 start time has not been announced", display.AccessibleText);
    }

    [Fact]
    public void Awaiting_schedule_with_nothing_known()
    {
        var feed = TestFeeds.Feed();

        var display = DisplayAt(RaceStart, feed);

        Assert.Equal(CountdownDisplay.DefaultTitle, display.Title);
        Assert.Equal("", display.Subtitle);
        Assert.Equal("Start time to be announced", display.Detail);
    }

    [Fact]
    public void Footer_before_the_first_download()
    {
        Assert.Equal(
            "Using built-in race data from 27 Sept, 10:00 am",
            DisplayAt(RaceStart.AddDays(-5), cacheState: FeedCacheState.Empty).Footer);
    }

    [Fact]
    public void Footer_when_stale()
    {
        var failing = new FeedCacheState(LastAttemptUtc: RaceStart.AddDays(-1), LastRefreshFailed: true);
        var oldFeed = TestFeeds.Feed(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero), TestFeeds.Bathurst2026());

        var display = DisplayAt(RaceStart.AddDays(-2), oldFeed, failing);

        Assert.True(display.IsStale);
        Assert.Equal("Offline — race data from 1 Sept, 10:00 am", display.Footer);
    }

    [Fact]
    public void Footer_with_no_feed()
    {
        var empty = TestFeeds.Feed();
        var state = CountdownCalculator.Calculate(empty, EventFilter.Bathurst1000MainRace, RaceStart);

        var display = CountdownDisplay.Create(state, feed: null, FeedCacheState.Empty, Sydney, AuCulture);

        Assert.Equal("No race data yet. Check your connection.", display.Footer);
    }
}
