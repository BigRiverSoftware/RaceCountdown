using RaceCountdown.Core.Countdown;
using RaceCountdown.Core.Models;

namespace RaceCountdown.Core.Tests;

public class NextSessionSelectorTests
{
    private static readonly EventFilter Filter = EventFilter.Bathurst1000MainRace;
    private static readonly DateTimeOffset RaceStart = TestFeeds.RaceStart2026;

    [Fact]
    public void Picks_the_2026_main_race()
    {
        var target = NextSessionSelector.Select(TestFeeds.Feed(TestFeeds.Bathurst2026()), Filter, RaceStart.AddDays(-14));

        Assert.NotNull(target);
        Assert.Equal("supercars-2026-bathurst-1000", target.Event.Id);
        Assert.Equal("race-30", target.Session.Id);
    }

    [Fact]
    public void Keeps_the_race_while_it_is_running_then_rolls_over_to_the_next_year()
    {
        var next = TestFeeds.Bathurst(2027, new DateTimeOffset(2027, 10, 10, 0, 30, 0, TimeSpan.Zero));
        var feed = TestFeeds.Feed(TestFeeds.Bathurst2026(), next);

        Assert.Equal("supercars-2026-bathurst-1000", NextSessionSelector.Select(feed, Filter, RaceStart.AddHours(6.99))!.Event.Id);
        Assert.Equal("supercars-2027-bathurst-1000", NextSessionSelector.Select(feed, Filter, RaceStart.AddHours(7))!.Event.Id);
    }

    [Fact]
    public void Returns_null_when_every_race_has_finished()
    {
        Assert.Null(NextSessionSelector.Select(TestFeeds.Feed(TestFeeds.Bathurst2026()), Filter, RaceStart.AddDays(1)));
    }

    [Fact]
    public void Treats_a_session_without_a_duration_as_finished_at_its_start()
    {
        var ev = TestFeeds.Bathurst2026();
        ev = ev with { Sessions = [ev.Sessions[0] with { EstimatedDuration = null }] };
        var feed = TestFeeds.Feed(ev);

        Assert.NotNull(NextSessionSelector.Select(feed, Filter, RaceStart.AddTicks(-1)));
        Assert.Null(NextSessionSelector.Select(feed, Filter, RaceStart));
    }

    [Theory]
    [InlineData(EventStatus.Provisional)]
    [InlineData(EventStatus.DateTba)]
    [InlineData(EventStatus.Cancelled)]
    public void Ignores_events_that_are_not_confirmed(EventStatus status)
    {
        var feed = TestFeeds.Feed(TestFeeds.Bathurst(2026, RaceStart, status));

        Assert.Null(NextSessionSelector.Select(feed, Filter, RaceStart.AddDays(-1)));
    }

    [Fact]
    public void Ignores_other_session_types_other_events_and_other_series()
    {
        var bathurst = TestFeeds.Bathurst2026();
        bathurst = bathurst with
        {
            Sessions = [TestFeeds.Race(RaceStart.AddDays(-2), "qualifying", type: SessionType.Qualifying), .. bathurst.Sessions],
        };
        var sandown = TestFeeds.Bathurst(2026, RaceStart.AddDays(-28), name: "Penrite Sandown 500") with { Id = "supercars-2026-sandown" };
        var super2 = TestFeeds.Bathurst2026() with { Id = "super2-2026-bathurst", SeriesId = "super2" };

        var target = NextSessionSelector.Select(TestFeeds.Feed(sandown, super2, bathurst), Filter, RaceStart.AddDays(-30));

        Assert.Equal("supercars-2026-bathurst-1000", target!.Event.Id);
        Assert.Equal("race-30", target.Session.Id);
    }

    [Fact]
    public void Picks_the_earliest_of_several_matching_sessions()
    {
        var anyRace = new EventFilter(SessionType: SessionType.Race);
        var ev = TestFeeds.Bathurst2026();
        ev = ev with { Sessions = [ev.Sessions[0], TestFeeds.Race(RaceStart.AddDays(-1), "race-29")] };

        Assert.Equal("race-29", NextSessionSelector.Select(TestFeeds.Feed(ev), anyRace, RaceStart.AddDays(-3))!.Session.Id);
    }

    [Fact]
    public void Finds_the_upcoming_tba_event_with_its_dates()
    {
        var tba = TestFeeds.Tba(2027, new DateOnly(2027, 10, 7), new DateOnly(2027, 10, 10));
        var feed = TestFeeds.Feed(TestFeeds.Bathurst2026(), tba);

        Assert.Same(tba, NextSessionSelector.SelectUpcomingUnscheduled(feed, Filter, RaceStart.AddDays(2)));
    }

    [Fact]
    public void Prefers_the_dated_tba_event_over_one_without_dates()
    {
        var undated = TestFeeds.Tba(2028);
        var dated = TestFeeds.Tba(2027, new DateOnly(2027, 10, 7), new DateOnly(2027, 10, 10));

        Assert.Same(dated, NextSessionSelector.SelectUpcomingUnscheduled(TestFeeds.Feed(undated, dated), Filter, RaceStart));
    }

    [Fact]
    public void Ignores_tba_events_that_have_already_ended()
    {
        var old = TestFeeds.Tba(2026, new DateOnly(2026, 10, 8), new DateOnly(2026, 10, 11));

        Assert.Null(NextSessionSelector.SelectUpcomingUnscheduled(TestFeeds.Feed(old), Filter, RaceStart.AddDays(3)));
    }

    [Fact]
    public void Filter_matches_event_names_case_insensitively_and_null_criteria_match_anything()
    {
        var ev = TestFeeds.Bathurst2026() with { Name = "The BATHURST 1000" };

        Assert.True(EventFilter.Bathurst1000MainRace.MatchesEvent(ev));
        Assert.True(new EventFilter().MatchesEvent(ev));
        Assert.True(new EventFilter().MatchesSession(ev.Sessions[0]));
        Assert.False(new EventFilter(TrackId: "the-bend").MatchesEvent(ev));
        Assert.False(new EventFilter(EventNameContains: "Gold Coast").MatchesEvent(ev));
    }
}
