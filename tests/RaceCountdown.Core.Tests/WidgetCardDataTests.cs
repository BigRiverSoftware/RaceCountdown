using System.Text.Json;
using RaceCountdown.Core.Countdown;
using RaceCountdown.Core.Widgets;

namespace RaceCountdown.Core.Tests;

public class WidgetCardDataTests
{
    private static readonly DateTimeOffset RaceStart = TestFeeds.RaceStart2026;

    private static JsonElement DataAt(DateTimeOffset now, bool lastRefreshFailed = false, string? background = null, Models.EventFeed? feed = null)
    {
        var state = CountdownCalculator.Calculate(
            feed ?? TestFeeds.Feed(TestFeeds.Bathurst2026()), EventFilter.Bathurst1000MainRace, now, lastRefreshFailed);
        var json = WidgetCardData.ToJson(WidgetSnapshot.Create(state, "Bathurst 1000"), background);
        return JsonDocument.Parse(json).RootElement;
    }

    [Fact]
    public void Counting_has_every_text_the_templates_bind()
    {
        var data = DataAt(RaceStart - new TimeSpan(13, 4, 7, 30));

        Assert.Equal("Repco Bathurst 1000", data.GetProperty("title").GetString());
        Assert.Equal("13 days", data.GetProperty("headline").GetString());
        Assert.Equal("04h 07m", data.GetProperty("detail").GetString());
        Assert.Equal("13", data.GetProperty("compactHeadline").GetString());
        Assert.Equal("DAYS TO GO", data.GetProperty("compactCaption").GetString());
        Assert.Equal("13 days, 4 hours until the Repco Bathurst 1000", data.GetProperty("accessibleText").GetString());
        Assert.False(data.GetProperty("isStale").GetBoolean());
        Assert.Equal(WidgetCardData.StaleText, data.GetProperty("staleText").GetString());
        Assert.Equal("", data.GetProperty("background").GetString());
    }

    [Fact]
    public void Race_day_and_live_texts()
    {
        var raceDay = DataAt(RaceStart - new TimeSpan(4, 12, 0));
        Assert.Equal(("04:12", "RACE DAY"), (raceDay.GetProperty("headline").GetString(), raceDay.GetProperty("detail").GetString()));

        var live = DataAt(RaceStart + TimeSpan.FromMinutes(65));
        Assert.Equal("RACE UNDERWAY", live.GetProperty("headline").GetString());
        Assert.Equal("Green flag 1:05 ago", live.GetProperty("detail").GetString());
        Assert.Equal(("LIVE", "RACE UNDERWAY"), (live.GetProperty("compactHeadline").GetString(), live.GetProperty("compactCaption").GetString()));
    }

    [Fact]
    public void Awaiting_schedule_shows_tba()
    {
        var data = DataAt(RaceStart.AddDays(30), feed: TestFeeds.Feed(TestFeeds.Tba(2027, new DateOnly(2027, 10, 7), new DateOnly(2027, 10, 10))));

        Assert.Equal("TBA", data.GetProperty("headline").GetString());
        Assert.Equal("7–10 Oct 2027", data.GetProperty("detail").GetString());
    }

    [Fact]
    public void Stale_flag_and_background_are_passed_through()
    {
        // A failed refresh with a feed generated more than 14 days before now makes the countdown stale.
        var oldFeed = TestFeeds.Feed(new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero), TestFeeds.Bathurst2026());
        var data = DataAt(RaceStart - TimeSpan.FromDays(2), lastRefreshFailed: true, background: "data:image/png;base64,AAAA", feed: oldFeed);

        Assert.True(data.GetProperty("isStale").GetBoolean());
        Assert.Equal("data:image/png;base64,AAAA", data.GetProperty("background").GetString());
    }

    [Fact]
    public void Text_is_json_escaped()
    {
        var snapshot = new WidgetSnapshot(CountdownPhase.AwaitingSchedule, "Quote \" and \\ slash", null, "TBA", "d", "d", "a", null, null, false);

        var data = JsonDocument.Parse(WidgetCardData.ToJson(snapshot, null)).RootElement;

        Assert.Equal("Quote \" and \\ slash", data.GetProperty("title").GetString());
    }
}
