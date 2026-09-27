using RaceCountdown.Core.Models;

namespace EventFeedBuilder.Tests;

public class FeedBuilderTests
{
    private static readonly CalendarEntry Bathurst2026 = new(
        "2026-bathurst-1000", "2026 Repco Bathurst 1000", "Bathurst, NSW",
        new DateTimeOffset(2026, 10, 8, 6, 0, 0, TimeSpan.FromHours(11)),
        new DateTimeOffset(2026, 10, 11, 18, 0, 0, TimeSpan.FromHours(11)));

    private static readonly SourceSession MainRace = new(
        "Race 30", "Race", FeedBuilder.MainSeriesName,
        new DateTimeOffset(2026, 10, 11, 11, 30, 0, TimeSpan.FromHours(11)),
        new DateTimeOffset(2026, 10, 11, 18, 30, 0, TimeSpan.FromHours(11)));

    [Fact]
    public async Task Builds_the_2026_race_from_the_saved_pages()
    {
        // Phase 1 "done" criterion: the 2026 Bathurst 1000 race at 2026-10-11 11:30 Australia/Sydney.
        var source = new DirectorySupercarsSource(Fixtures.Directory);
        var calendar = SupercarsParser.ParseCalendar(await source.GetCalendarAsync(CancellationToken.None));
        var page = await source.GetEventPageAsync("2026-bathurst-1000", CancellationToken.None);
        var sessions = new Dictionary<string, IReadOnlyList<SourceSession>> { ["2026-bathurst-1000"] = SupercarsParser.ParseEventSessions(page!) };

        var feed = FeedBuilder.Build(calendar, sessions, Fixtures.Overrides(), Fixtures.SavedOn);

        var ev = Assert.Single(feed.Events);
        Assert.Equal("supercars-2026-bathurst-1000", ev.Id);
        Assert.Equal("Repco Bathurst 1000", ev.Name);
        Assert.Equal(EventStatus.Confirmed, ev.Status);
        Assert.Equal((new DateOnly(2026, 10, 8), new DateOnly(2026, 10, 11)), (ev.StartDate!.Value, ev.EndDate!.Value));
        var race = Assert.Single(ev.Sessions);
        Assert.Equal(("race-30", "Race 30", SessionType.Race), (race.Id, race.Name, race.Type));
        Assert.Equal("2026-10-11T11:30:00", race.StartLocal);
        Assert.Equal(new DateTimeOffset(2026, 10, 11, 0, 30, 0, TimeSpan.Zero), race.StartUtc);
        Assert.Equal(TimeSpan.FromHours(7), race.EstimatedDuration);
        Assert.Equal(Fixtures.SavedOn, feed.GeneratedUtc);
        Assert.Equal("mount-panorama", Assert.Single(feed.Tracks).Id);
    }

    [Fact]
    public async Task Directory_source_returns_null_for_a_missing_page()
    {
        Assert.Null(await new DirectorySupercarsSource(Fixtures.Directory).GetEventPageAsync("2099-bathurst-1000", CancellationToken.None));
    }

    [Fact]
    public void An_event_without_a_main_race_is_tba_but_keeps_its_dates()
    {
        var supportOnly = MainRace with { SeriesName = "DUNLOP Super2 Series" };
        var sessions = new Dictionary<string, IReadOnlyList<SourceSession>> { [Bathurst2026.Slug] = [supportOnly] };

        var ev = Assert.Single(FeedBuilder.Build([Bathurst2026], sessions, Fixtures.Overrides(), Fixtures.SavedOn).Events);

        Assert.Equal(EventStatus.DateTba, ev.Status);
        Assert.Empty(ev.Sessions);
        Assert.Equal(new DateOnly(2026, 10, 8), ev.StartDate);
    }

    [Fact]
    public void A_missing_event_page_is_tba()
    {
        var ev = Assert.Single(FeedBuilder.Build([Bathurst2026], new Dictionary<string, IReadOnlyList<SourceSession>>(), Fixtures.Overrides(), Fixtures.SavedOn).Events);

        Assert.Equal(EventStatus.DateTba, ev.Status);
    }

    [Fact]
    public void A_session_without_a_usable_end_has_no_duration()
    {
        var sessions = new Dictionary<string, IReadOnlyList<SourceSession>> { [Bathurst2026.Slug] = [MainRace with { End = MainRace.Start }] };

        var race = FeedBuilder.Build([Bathurst2026], sessions, Fixtures.Overrides(), Fixtures.SavedOn).Events[0].Sessions[0];

        Assert.Null(race.EstimatedDuration);
    }

    [Fact]
    public void Adds_an_undated_tba_placeholder_once_the_last_listed_race_is_over()
    {
        var sessions = new Dictionary<string, IReadOnlyList<SourceSession>> { [Bathurst2026.Slug] = [MainRace] };
        var november = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);

        var feed = FeedBuilder.Build([Bathurst2026], sessions, Fixtures.Overrides(), november);

        Assert.Equal(["supercars-2026-bathurst-1000", "supercars-2027-bathurst-1000"], feed.Events.Select(e => e.Id));
        var placeholder = feed.Events[1];
        Assert.Equal(EventStatus.DateTba, placeholder.Status);
        Assert.Null(placeholder.StartDate);
        Assert.Empty(placeholder.Sessions);
    }

    [Fact]
    public void Adds_a_placeholder_for_this_year_when_the_calendar_has_no_bathurst()
    {
        var gold = Bathurst2026 with { Slug = "2026-gold-coast", Title = "2026 Gold Coast 500" };

        var ev = Assert.Single(FeedBuilder.Build([gold], new Dictionary<string, IReadOnlyList<SourceSession>>(), Fixtures.Overrides(), Fixtures.SavedOn).Events);

        Assert.Equal("supercars-2026-bathurst-1000", ev.Id);
        Assert.Equal(EventStatus.DateTba, ev.Status);
    }

    [Fact]
    public void An_override_replaces_the_scraped_event_with_the_same_id()
    {
        var sessions = new Dictionary<string, IReadOnlyList<SourceSession>> { [Bathurst2026.Slug] = [MainRace] };
        var baseOverrides = Fixtures.Overrides();
        var delayed = new Session("race-30", "Race 30", SessionType.Race,
            new DateTimeOffset(2026, 10, 11, 2, 0, 0, TimeSpan.Zero), "2026-10-11T13:00:00", TimeSpan.FromHours(7));
        var replacement = new RaceEvent("supercars-2026-bathurst-1000", "supercars", "mount-panorama", "Repco Bathurst 1000",
            new DateOnly(2026, 10, 8), new DateOnly(2026, 10, 11), EventStatus.Confirmed, [delayed]);

        var feed = FeedBuilder.Build([Bathurst2026], sessions, baseOverrides with { Events = [replacement] }, Fixtures.SavedOn);

        Assert.Same(replacement, Assert.Single(feed.Events));
    }

    [Fact]
    public void Fails_without_the_track_in_the_overrides()
    {
        var overrides = Fixtures.Overrides() with { Tracks = [] };

        Assert.Throws<InvalidOperationException>(
            () => FeedBuilder.Build([Bathurst2026], new Dictionary<string, IReadOnlyList<SourceSession>>(), overrides, Fixtures.SavedOn));
    }

    [Theory]
    [InlineData("Race 30", "race-30")]
    [InlineData("Boost Mobile Qualifying (Race 30)", "boost-mobile-qualifying-race-30")]
    [InlineData("  Top 10 Shootout!  ", "top-10-shootout")]
    public void Slugify(string name, string expected)
    {
        Assert.Equal(expected, FeedBuilder.Slugify(name));
    }

    [Fact]
    public void Overrides_file_parses_with_comments()
    {
        var overrides = Fixtures.Overrides();

        Assert.Equal("Australia/Sydney", Assert.Single(overrides.Tracks).TimeZoneId);
        Assert.Equal("supercars", Assert.Single(overrides.Series).Id);
        Assert.Empty(overrides.Events);
    }

    [Fact]
    public void Overrides_reject_null_and_missing_fields()
    {
        Assert.ThrowsAny<System.Text.Json.JsonException>(() => FeedOverrides.Parse("null"));
        Assert.ThrowsAny<System.Text.Json.JsonException>(() => FeedOverrides.Parse("{ \"series\": [] }"));
    }
}
