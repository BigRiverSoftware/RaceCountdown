using RaceCountdown.Core.Countdown;
using RaceCountdown.Core.Widgets;

namespace RaceCountdown.Core.Tests;

public class WidgetSnapshotTests
{
    private static readonly DateTimeOffset RaceStart = TestFeeds.RaceStart2026;

    private static WidgetSnapshot SnapshotAt(DateTimeOffset now, params Models.RaceEvent[] events)
    {
        var feed = events.Length == 0 ? TestFeeds.Feed(TestFeeds.Bathurst2026()) : TestFeeds.Feed(events);
        return WidgetSnapshot.Create(CountdownCalculator.Calculate(feed, EventFilter.Bathurst1000MainRace, now), "Bathurst 1000");
    }

    [Fact]
    public void Counting()
    {
        var snapshot = SnapshotAt(RaceStart - new TimeSpan(13, 4, 7, 30));

        Assert.Equal(CountdownPhase.Counting, snapshot.Phase);
        Assert.Equal("Repco Bathurst 1000", snapshot.Title);
        Assert.Equal(13, snapshot.Days);
        Assert.Equal("13 days", snapshot.Headline);
        Assert.Equal("04h 07m", snapshot.Detail);
        Assert.Equal("13 days, 4 hours until the Repco Bathurst 1000", snapshot.AccessibleText);
        Assert.Equal(RaceStart, snapshot.TargetUtc);
        Assert.Equal(RaceStart.AddHours(-24), snapshot.NextPhaseChangeUtc);
        Assert.False(snapshot.IsStale);
    }

    [Fact]
    public void Counting_uses_singular_units()
    {
        var snapshot = SnapshotAt(RaceStart - new TimeSpan(1, 1, 0, 0));

        Assert.Equal("1 day", snapshot.Headline);
        Assert.Equal("1 day, 1 hour until the Repco Bathurst 1000", snapshot.AccessibleText);
    }

    [Fact]
    public void Race_day()
    {
        var snapshot = SnapshotAt(RaceStart - new TimeSpan(4, 12, 0));

        Assert.Equal(CountdownPhase.RaceDay, snapshot.Phase);
        Assert.Null(snapshot.Days);
        Assert.Equal("04:12", snapshot.Headline);
        Assert.Equal("RACE DAY", snapshot.Detail);
        Assert.Equal("Race day: 4 hours, 12 minutes until the Repco Bathurst 1000", snapshot.AccessibleText);
        Assert.Equal(RaceStart, snapshot.NextPhaseChangeUtc);
    }

    [Fact]
    public void Race_day_at_exactly_24_hours_shows_24_hours()
    {
        Assert.Equal("24:00", SnapshotAt(RaceStart.AddHours(-24)).Headline);
    }

    [Fact]
    public void Live()
    {
        var snapshot = SnapshotAt(RaceStart + new TimeSpan(1, 23, 0));

        Assert.Equal(CountdownPhase.Live, snapshot.Phase);
        Assert.Equal("RACE UNDERWAY", snapshot.Headline);
        Assert.Equal("Green flag 1:23 ago", snapshot.Detail);
        Assert.Equal("The Repco Bathurst 1000 is underway", snapshot.AccessibleText);
        Assert.Equal(RaceStart.AddHours(7), snapshot.NextPhaseChangeUtc);
    }

    [Fact]
    public void Awaiting_schedule_with_dates_in_one_month()
    {
        var tba = TestFeeds.Tba(2027, new DateOnly(2027, 10, 7), new DateOnly(2027, 10, 10));

        var snapshot = SnapshotAt(RaceStart.AddDays(1), TestFeeds.Bathurst2026(), tba);

        Assert.Equal(CountdownPhase.AwaitingSchedule, snapshot.Phase);
        Assert.Equal("Bathurst 1000", snapshot.Title);
        Assert.Equal("TBA", snapshot.Headline);
        Assert.Equal("7–10 Oct 2027", snapshot.Detail);
        Assert.Null(snapshot.TargetUtc);
        Assert.Null(snapshot.NextPhaseChangeUtc);
    }

    [Fact]
    public void Awaiting_schedule_with_dates_across_months()
    {
        var tba = TestFeeds.Tba(2027, new DateOnly(2027, 9, 30), new DateOnly(2027, 10, 3));

        Assert.Equal("30 Sep–3 Oct 2027", SnapshotAt(RaceStart.AddDays(1), tba).Detail);
    }

    [Fact]
    public void Awaiting_schedule_without_dates_uses_the_fallback_title()
    {
        var snapshot = SnapshotAt(RaceStart.AddDays(1), TestFeeds.Bathurst2026());

        Assert.Equal("Bathurst 1000", snapshot.Title);
        Assert.Equal("Start time to be announced", snapshot.Detail);
        Assert.Equal("The next Bathurst 1000 start time has not been announced", snapshot.AccessibleText);
    }

    [Fact]
    public void Carries_the_stale_flag()
    {
        var feed = TestFeeds.Feed(RaceStart.AddDays(-30), TestFeeds.Bathurst2026());
        var state = CountdownCalculator.Calculate(feed, EventFilter.Bathurst1000MainRace, RaceStart.AddDays(-2), lastRefreshFailed: true);

        Assert.True(WidgetSnapshot.Create(state, "Bathurst 1000").IsStale);
    }
}
