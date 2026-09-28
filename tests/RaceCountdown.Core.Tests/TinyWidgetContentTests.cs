using RaceCountdown.Core.Countdown;
using RaceCountdown.Core.Models;
using RaceCountdown.Core.Widgets;

namespace RaceCountdown.Core.Tests;

public class TinyWidgetContentTests
{
    // 11 Oct 2026, 11:30 in Sydney (AEDT, UTC+11).
    private static readonly DateTimeOffset RaceStart = TestFeeds.RaceStart2026;

    private static WidgetSnapshot SnapshotAt(DateTimeOffset now, EventFeed? feed = null) =>
        WidgetSnapshot.Create(
            CountdownCalculator.Calculate(feed ?? TestFeeds.Feed(TestFeeds.Bathurst2026()), EventFilter.Bathurst1000MainRace, now),
            "Bathurst 1000");

    private static TinyWidgetContent ContentAt(DateTimeOffset now, EventFeed? feed = null) =>
        TinyWidgetContent.Create(SnapshotAt(now, feed), now);

    [Fact]
    public void Days_match_the_other_widgets_not_the_calendar_dates()
    {
        // 21:00 on 28 Sep in Sydney (AEST): 13 calendar dates before 11 Oct, but 12 days 13½ hours to go.
        var now = new DateTimeOffset(2026, 9, 28, 21, 0, 0, TimeSpan.FromHours(10));

        var content = ContentAt(now);

        Assert.Equal(TinyCountdownMode.Days, content.Mode);
        Assert.Equal(("12", "DAYS"), (content.Headline, content.Caption));
        Assert.Equal(SnapshotAt(now).DaysText, content.Headline);
        Assert.Equal("12 days until the Repco Bathurst 1000", content.AccessibleText);
        Assert.False(content.UsesChronometer);
    }

    [Fact]
    public void Days_redraw_just_after_the_days_shown_drop()
    {
        var now = RaceStart - new TimeSpan(12, 13, 30, 0);

        var next = ContentAt(now).NextRedrawUtc!.Value;

        Assert.Equal(RaceStart - TimeSpan.FromDays(12) + TimeSpan.FromSeconds(1), next);
        Assert.Equal("12", ContentAt(next - TimeSpan.FromSeconds(1)).Headline);
        Assert.Equal("11", ContentAt(next).Headline);
    }

    [Fact]
    public void Last_day_before_race_day_is_singular_and_redraws_when_race_day_starts()
    {
        var now = RaceStart - TimeSpan.FromHours(30);

        var content = ContentAt(now);

        Assert.Equal(("1", "DAY"), (content.Headline, content.Caption));
        Assert.Equal(RaceStart - TimeSpan.FromHours(24), content.NextRedrawUtc);
    }

    [Theory]
    [InlineData(24 * 60, "24:00")]
    [InlineData(15 * 60 + 30, "15:30")]
    public void Final_24_hours_show_hours_and_minutes_like_the_other_widgets_race_day(int minutesToGo, string headline)
    {
        var content = ContentAt(RaceStart - TimeSpan.FromMinutes(minutesToGo));

        Assert.Equal(TinyCountdownMode.HoursMinutes, content.Mode);
        Assert.Equal((headline, "TO GO"), (content.Headline, content.Caption));
    }

    [Fact]
    public void Hours_and_minutes_redraw_when_the_minutes_drop()
    {
        var now = RaceStart - new TimeSpan(4, 12, 20);

        var content = ContentAt(now);

        Assert.Equal(("04:12", "TO GO"), (content.Headline, content.Caption));
        Assert.Equal("Race day: 4 hours, 12 minutes until the Repco Bathurst 1000", content.AccessibleText);
        Assert.Equal(RaceStart - new TimeSpan(4, 12, 0) + TimeSpan.FromSeconds(1), content.NextRedrawUtc);
        Assert.Equal("04:11", ContentAt(content.NextRedrawUtc!.Value).Headline);
    }

    [Fact]
    public void Hours_and_minutes_redraw_no_later_than_the_final_countdown()
    {
        var now = RaceStart - new TimeSpan(0, 30, 30);

        var content = ContentAt(now);

        Assert.Equal(TinyCountdownMode.HoursMinutes, content.Mode);
        Assert.Equal("00:30", content.Headline);
        Assert.Equal(RaceStart - TinyWidgetContent.FinalCountdown, content.NextRedrawUtc);
    }

    [Theory]
    [InlineData(30 * 60, "30:00")]
    [InlineData(29 * 60 + 45, "29:45")]
    [InlineData(1, "00:01")]
    public void Final_30_minutes_tick_minutes_and_seconds_until_the_start(int secondsToGo, string headline)
    {
        var content = ContentAt(RaceStart - TimeSpan.FromSeconds(secondsToGo));

        Assert.Equal(TinyCountdownMode.MinutesSeconds, content.Mode);
        Assert.True(content.UsesChronometer);
        Assert.Equal((headline, "TO GO"), (content.Headline, content.Caption));
        Assert.Equal(RaceStart, content.TargetUtc);
        Assert.Equal(RaceStart, content.NextRedrawUtc);
        Assert.Equal("The Repco Bathurst 1000 starts in under 30 minutes", content.AccessibleText);
    }

    [Fact]
    public void Live_and_awaiting_schedule_show_a_word()
    {
        var live = ContentAt(RaceStart + TimeSpan.FromHours(1));
        Assert.Equal((TinyCountdownMode.Message, "LIVE", "RACE ON"), (live.Mode, live.Headline, live.Caption));
        Assert.Null(live.NextRedrawUtc);
        Assert.Null(live.TargetUtc);

        var tba = ContentAt(RaceStart.AddDays(30), TestFeeds.Feed(TestFeeds.Tba(2027)));
        Assert.Equal((TinyCountdownMode.Message, "TBA", "NEXT RACE"), (tba.Mode, tba.Headline, tba.Caption));
        Assert.Null(tba.NextRedrawUtc);
    }
}
