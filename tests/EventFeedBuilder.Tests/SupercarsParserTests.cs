namespace EventFeedBuilder.Tests;

/// <summary>
/// Snapshot tests against pages saved from supercars.com on 2026-09-27. If the site changes its markup,
/// these fail here instead of a bad feed being published.
/// </summary>
public class SupercarsParserTests
{
    [Fact]
    public void Calendar_lists_the_whole_2026_season_once_each()
    {
        var calendar = SupercarsParser.ParseCalendar(Fixtures.Read("calendar.html"));

        Assert.Equal(14, calendar.Count);
        Assert.Equal(calendar.Count, calendar.Select(e => e.Slug).Distinct().Count());
        Assert.Equal(calendar.OrderBy(e => e.Start), calendar);
    }

    [Fact]
    public void Calendar_has_the_bathurst_1000_with_offset_dates()
    {
        var bathurst = Assert.Single(FeedBuilder.BathurstEntries(SupercarsParser.ParseCalendar(Fixtures.Read("calendar.html"))));

        Assert.Equal("2026-bathurst-1000", bathurst.Slug);
        Assert.Equal("2026 Repco Bathurst 1000", bathurst.Title);
        Assert.Equal("Bathurst, NSW", bathurst.Location);
        Assert.Equal(new DateTimeOffset(2026, 10, 8, 6, 0, 0, TimeSpan.FromHours(11)), bathurst.Start);
        Assert.Equal(new DateTimeOffset(2026, 10, 11, 18, 0, 0, TimeSpan.FromHours(11)), bathurst.End);
    }

    [Fact]
    public void Event_page_has_exactly_one_main_race()
    {
        var sessions = SupercarsParser.ParseEventSessions(Fixtures.Read("2026-bathurst-1000.html"));

        var race = Assert.Single(sessions, s => s.SeriesName == FeedBuilder.MainSeriesName && s.Type == "Race");
        Assert.Equal("Race 30", race.Name);
        Assert.Equal(new DateTimeOffset(2026, 10, 11, 11, 30, 0, TimeSpan.FromHours(11)), race.Start);
        Assert.Equal(new DateTimeOffset(2026, 10, 11, 18, 30, 0, TimeSpan.FromHours(11)), race.End);
    }

    [Fact]
    public void Event_page_includes_support_categories_and_other_session_types()
    {
        var sessions = SupercarsParser.ParseEventSessions(Fixtures.Read("2026-bathurst-1000.html"));

        Assert.Contains(sessions, s => s.SeriesName == "DUNLOP Super2 Series" && s.Type == "Race");
        Assert.Contains(sessions, s => s.SeriesName == FeedBuilder.MainSeriesName && s.Type == "Qualifying");
        Assert.Contains(sessions, s => s.SeriesName == "DUNLOP Super2 Series" && s.Type == "Shootout");
        Assert.Equal(sessions.Count, sessions.Distinct().Count());
    }

    [Fact]
    public void A_not_yet_published_event_page_has_no_sessions()
    {
        // supercars.com answers 200 with an empty page for events it does not know yet.
        Assert.Empty(SupercarsParser.ParseEventSessions(Fixtures.Read("2027-bathurst-1000.html")));
    }

    [Fact]
    public void Skips_lines_that_are_not_json_and_reads_nested_objects()
    {
        const string html = """
            <script>self.__next_f.push([1,"0:[\"$\",\"div\",null]\n1:not json {\n"])</script>
            <script>self.__next_f.push([1,"2:{\"a\":[{\"slug\":\"x\",\"title\":\"2030 X\",\"startDate\":\"2030-01-01T10:00:00.000+11:00\",\"endDate\":\"2030-01-02T10:00:00+11:00\"}]}\n"])</script>
            """;

        var entry = Assert.Single(SupercarsParser.ParseCalendar(html));

        Assert.Equal("x", entry.Slug);
        Assert.Null(entry.Location);
        Assert.Equal(TimeSpan.FromHours(11), entry.End.Offset);
    }

    [Fact]
    public void Rejects_dates_without_an_offset()
    {
        const string html = """
            <script>self.__next_f.push([1,"2:{\"slug\":\"x\",\"title\":\"X\",\"startDate\":\"2030-01-01T10:00:00\",\"endDate\":\"2030-01-02T10:00:00\"}\n"])</script>
            """;

        Assert.Throws<FormatException>(() => SupercarsParser.ParseCalendar(html));
    }

    [Fact]
    public void Returns_nothing_for_a_page_without_next_data()
    {
        Assert.Empty(SupercarsParser.ParseCalendar("<html><body>Maintenance</body></html>"));
    }
}
