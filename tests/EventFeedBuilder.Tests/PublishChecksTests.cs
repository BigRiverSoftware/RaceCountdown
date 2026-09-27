using Json.Schema;
using RaceCountdown.Core.Feed;
using RaceCountdown.Core.Models;

namespace EventFeedBuilder.Tests;

public class PublishChecksTests
{
    private static readonly JsonSchema Schema = JsonSchema.FromText(File.ReadAllText(Fixtures.FeedFile("events.schema.json")));
    private static readonly IReadOnlySet<string> NoOverrides = new HashSet<string>();

    private static EventFeed BuildFixtureFeed(DateTimeOffset now)
    {
        var sessions = new Dictionary<string, IReadOnlyList<SourceSession>>
        {
            ["2026-bathurst-1000"] = SupercarsParser.ParseEventSessions(Fixtures.Read("2026-bathurst-1000.html")),
        };
        return FeedBuilder.Build(SupercarsParser.ParseCalendar(Fixtures.Read("calendar.html")), sessions, Fixtures.Overrides(), now);
    }

    [Fact]
    public void The_fixture_feed_passes_every_check()
    {
        var feed = BuildFixtureFeed(Fixtures.SavedOn);

        Assert.Empty(PublishChecks.Run(FeedSerializer.Serialize(feed), feed, Schema, previous: feed, NoOverrides, Fixtures.SavedOn));
    }

    [Fact]
    public void A_between_seasons_feed_with_a_placeholder_passes()
    {
        var november = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
        var feed = BuildFixtureFeed(november);

        Assert.Empty(PublishChecks.Run(FeedSerializer.Serialize(feed), feed, Schema, previous: null, NoOverrides, november));
    }

    [Fact]
    public void Schema_errors_are_reported()
    {
        var feed = BuildFixtureFeed(Fixtures.SavedOn);
        var json = FeedSerializer.Serialize(feed).Replace("\"accentColour\": \"#E10600\"", "\"accentColour\": \"red\"");

        Assert.Contains(PublishChecks.CheckSchema(json, Schema), e => e.Contains("/series/0/accentColour"));
    }

    [Fact]
    public void Schema_requires_dates_on_confirmed_events()
    {
        var feed = BuildFixtureFeed(Fixtures.SavedOn);
        var json = FeedSerializer.Serialize(feed).Replace("\"startDate\": \"2026-10-08\"", "\"startDate\": null");

        Assert.NotEmpty(PublishChecks.CheckSchema(json, Schema));
    }

    [Fact]
    public void Validator_errors_are_reported()
    {
        var feed = BuildFixtureFeed(Fixtures.SavedOn);
        feed = feed with { Tracks = [feed.Tracks[0] with { TimeZoneId = "Australia/Nowhere" }] };

        Assert.Contains(
            PublishChecks.Run(FeedSerializer.Serialize(feed), feed, Schema, null, NoOverrides, Fixtures.SavedOn),
            e => e.Contains("unknown time zone"));
    }

    [Fact]
    public void A_start_time_that_moves_more_than_a_week_is_rejected_unless_overridden()
    {
        var feed = BuildFixtureFeed(Fixtures.SavedOn);
        var ev = feed.Events[0];
        var race = ev.Sessions[0];
        var previous = feed with { Events = [ev with { Sessions = [race with { StartUtc = race.StartUtc.AddDays(-8) }] }] };

        Assert.Single(PublishChecks.CheckMoves(feed, previous, NoOverrides));
        Assert.Empty(PublishChecks.CheckMoves(feed, previous, new HashSet<string> { ev.Id }));
    }

    [Fact]
    public void A_small_move_is_allowed()
    {
        var feed = BuildFixtureFeed(Fixtures.SavedOn);
        var ev = feed.Events[0];
        var previous = feed with { Events = [ev with { Sessions = [ev.Sessions[0] with { StartUtc = ev.Sessions[0].StartUtc.AddHours(-2) }] }] };

        Assert.Empty(PublishChecks.CheckMoves(feed, previous, NoOverrides));
    }

    [Fact]
    public void A_feed_with_no_future_race_and_no_tba_event_is_rejected()
    {
        var feed = BuildFixtureFeed(Fixtures.SavedOn);
        var afterTheRace = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);

        Assert.Contains(
            PublishChecks.Run(FeedSerializer.Serialize(feed), feed, Schema, null, NoOverrides, afterTheRace),
            e => e.Contains("no future Bathurst 1000"));
    }

    [Theory]
    [InlineData(-8, false)]
    [InlineData(-6, true)]
    [InlineData(0, true)]
    [InlineData(6, true)]
    [InlineData(8, false)]
    public void Race_week_is_a_week_either_side_of_the_main_race(int daysFromRace, bool expected)
    {
        var feed = BuildFixtureFeed(Fixtures.SavedOn);
        var now = new DateTimeOffset(2026, 10, 11, 0, 30, 0, TimeSpan.Zero).AddDays(daysFromRace);

        Assert.Equal(expected, PublishChecks.IsRaceWeek(feed, now));
    }
}
