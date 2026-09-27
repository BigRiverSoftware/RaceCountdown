using Microsoft.Extensions.Time.Testing;
using RaceCountdown.Core.Countdown;

namespace RaceCountdown.Core.Tests;

public class CountdownCalculatorTests
{
    private static readonly EventFilter Filter = EventFilter.Bathurst1000MainRace;
    private static readonly DateTimeOffset RaceStart = TestFeeds.RaceStart2026;

    [Fact]
    public void Counts_down_across_the_nsw_daylight_saving_change()
    {
        // 1 Oct 2026 09:00 AEST (+10). DST starts on 4 Oct, so the local clocks jump an hour
        // before the race; the countdown runs on UTC instants and must not be affected.
        var now = new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.FromHours(10));

        var state = CountdownCalculator.Calculate(TestFeeds.Feed(TestFeeds.Bathurst2026()), Filter, now);

        Assert.Equal(CountdownPhase.Counting, state.Phase);
        Assert.Equal(new TimeSpan(10, 1, 30, 0), state.Remaining);
        Assert.Equal((10, 1, 30, 0), (state.Days, state.Hours, state.Minutes, state.Seconds));
        Assert.Equal("mount-panorama", state.Track!.Id);
        Assert.Equal(RaceStart.AddHours(-24), state.NextPhaseChangeUtc);
        Assert.False(state.IsStale);
    }

    [Fact]
    public void Uses_the_time_provider()
    {
        var time = new FakeTimeProvider(RaceStart.AddDays(-3));

        var state = CountdownCalculator.Calculate(TestFeeds.Feed(TestFeeds.Bathurst2026()), Filter, time);

        Assert.Equal(TimeSpan.FromDays(3), state.Remaining);
    }

    [Fact]
    public void Rounds_the_remaining_time_up_to_whole_seconds()
    {
        var state = CountdownCalculator.Calculate(TestFeeds.Feed(TestFeeds.Bathurst2026()), Filter, RaceStart.AddMilliseconds(-100));

        Assert.Equal(TimeSpan.FromSeconds(1), state.Remaining);
    }

    [Fact]
    public void Switches_to_race_day_at_exactly_24_hours_out()
    {
        var feed = TestFeeds.Feed(TestFeeds.Bathurst2026());

        Assert.Equal(CountdownPhase.Counting, CountdownCalculator.Calculate(feed, Filter, RaceStart.AddHours(-24).AddTicks(-1)).Phase);

        var raceDay = CountdownCalculator.Calculate(feed, Filter, RaceStart.AddHours(-24));
        Assert.Equal(CountdownPhase.RaceDay, raceDay.Phase);
        Assert.Equal(RaceStart, raceDay.NextPhaseChangeUtc);
    }

    [Fact]
    public void Is_live_from_the_start_until_the_estimated_finish()
    {
        var feed = TestFeeds.Feed(TestFeeds.Bathurst2026());

        var atStart = CountdownCalculator.Calculate(feed, Filter, RaceStart);
        Assert.Equal(CountdownPhase.Live, atStart.Phase);
        Assert.Equal(TimeSpan.Zero, atStart.Remaining);

        var later = CountdownCalculator.Calculate(feed, Filter, RaceStart.AddMinutes(90).AddMilliseconds(900));
        Assert.Equal(CountdownPhase.Live, later.Phase);
        Assert.Equal(TimeSpan.FromMinutes(90), later.Elapsed);
        Assert.Equal(RaceStart.AddHours(7), later.NextPhaseChangeUtc);
    }

    [Fact]
    public void Rolls_over_to_the_next_confirmed_race_after_the_finish()
    {
        var next = TestFeeds.Bathurst(2027, new DateTimeOffset(2027, 10, 10, 0, 30, 0, TimeSpan.Zero));

        var state = CountdownCalculator.Calculate(TestFeeds.Feed(TestFeeds.Bathurst2026(), next), Filter, RaceStart.AddHours(7));

        Assert.Equal(CountdownPhase.Counting, state.Phase);
        Assert.Equal("supercars-2027-bathurst-1000", state.Event!.Id);
    }

    [Fact]
    public void Awaits_the_schedule_and_shows_the_tba_event_after_the_last_race()
    {
        var tba = TestFeeds.Tba(2027, new DateOnly(2027, 10, 7), new DateOnly(2027, 10, 10));

        var state = CountdownCalculator.Calculate(TestFeeds.Feed(TestFeeds.Bathurst2026(), tba), Filter, RaceStart.AddDays(1));

        Assert.Equal(CountdownPhase.AwaitingSchedule, state.Phase);
        Assert.Same(tba, state.Event);
        Assert.Null(state.Session);
        Assert.Equal("mount-panorama", state.Track!.Id);
        Assert.Equal(TimeSpan.Zero, state.Remaining);
        Assert.Null(state.NextPhaseChangeUtc);
    }

    [Fact]
    public void Awaits_the_schedule_with_no_event_when_the_feed_has_nothing()
    {
        var state = CountdownCalculator.Calculate(TestFeeds.Feed(), Filter, RaceStart);

        Assert.Equal(CountdownPhase.AwaitingSchedule, state.Phase);
        Assert.Null(state.Event);
        Assert.Null(state.Track);
    }

    [Theory]
    [InlineData(15, true, true)]
    [InlineData(15, false, false)]
    [InlineData(13, true, false)]
    public void Is_stale_only_when_the_feed_is_old_and_the_refresh_failed(int feedAgeDays, bool refreshFailed, bool expected)
    {
        var now = RaceStart.AddDays(-5);
        var feed = TestFeeds.Feed(now.AddDays(-feedAgeDays), TestFeeds.Bathurst2026());

        var state = CountdownCalculator.Calculate(feed, Filter, now, refreshFailed);

        Assert.Equal(expected, state.IsStale);
        Assert.Equal(CountdownPhase.Counting, state.Phase);
    }
}
