using RaceCountdown.Core.Countdown;
using RaceCountdown.Core.Models;
using RaceCountdown.Core.Widgets;

namespace RaceCountdown.Core.Tests;

public class WidgetScheduleTests
{
    private static readonly DateTimeOffset RaceStart = TestFeeds.RaceStart2026;

    private static WidgetSnapshot SnapshotAt(DateTimeOffset now, EventFeed? feed = null) =>
        WidgetSnapshot.Create(
            CountdownCalculator.Calculate(feed ?? TestFeeds.Feed(TestFeeds.Bathurst2026()), EventFilter.Bathurst1000MainRace, now),
            "Bathurst 1000");

    [Fact]
    public void Counting_redraws_just_after_the_minutes_shown_drop()
    {
        var now = RaceStart - new TimeSpan(13, 4, 7, 30);

        var next = WidgetSchedule.NextRedrawUtc(SnapshotAt(now), now);

        Assert.Equal(RaceStart - new TimeSpan(13, 4, 7, 0) + TimeSpan.FromSeconds(1), next);
        Assert.Equal("07", SnapshotAt(next - TimeSpan.FromSeconds(1)).MinutesText);
        Assert.Equal("06", SnapshotAt(next).MinutesText);
    }

    [Fact]
    public void Counting_on_an_exact_minute_redraws_a_second_later()
    {
        var now = RaceStart - new TimeSpan(30, 5, 0);

        Assert.Equal(now + TimeSpan.FromSeconds(1), WidgetSchedule.NextRedrawUtc(SnapshotAt(now), now));
    }

    [Fact]
    public void Counting_just_before_race_day_redraws_at_the_phase_change()
    {
        var now = RaceStart - TimeSpan.FromHours(24) - TimeSpan.FromSeconds(30);

        Assert.Equal(RaceStart - TimeSpan.FromHours(24), WidgetSchedule.NextRedrawUtc(SnapshotAt(now), now));
    }

    [Fact]
    public void Race_day_uses_the_chronometer_and_redraws_at_the_start()
    {
        var now = RaceStart - TimeSpan.FromHours(5);
        var snapshot = SnapshotAt(now);

        Assert.True(WidgetSchedule.UsesChronometer(snapshot));
        Assert.Equal(RaceStart, WidgetSchedule.NextRedrawUtc(snapshot, now));
        Assert.Equal("RACE DAY", snapshot.ShortDetail);
    }

    [Fact]
    public void Live_redraws_every_half_hour_until_the_estimated_finish()
    {
        var now = RaceStart + TimeSpan.FromHours(1);
        var snapshot = SnapshotAt(now);

        Assert.False(WidgetSchedule.UsesChronometer(snapshot));
        Assert.Equal(now + WidgetSchedule.LiveInterval, WidgetSchedule.NextRedrawUtc(snapshot, now));
        Assert.Equal("The green flag has dropped", snapshot.ShortDetail);

        var nearEnd = RaceStart + TimeSpan.FromHours(6.9);
        Assert.Equal(RaceStart + TimeSpan.FromHours(7), WidgetSchedule.NextRedrawUtc(SnapshotAt(nearEnd), nearEnd));
    }

    [Fact]
    public void Awaiting_schedule_redraws_every_six_hours()
    {
        var feed = TestFeeds.Feed(TestFeeds.Tba(2027, new DateOnly(2027, 10, 7), new DateOnly(2027, 10, 10)));
        var now = RaceStart.AddDays(30);
        var snapshot = SnapshotAt(now, feed);

        Assert.Equal(now + WidgetSchedule.AwaitingInterval, WidgetSchedule.NextRedrawUtc(snapshot, now));
        Assert.Equal("7–10 Oct 2027", snapshot.ShortDetail);
    }

    [Fact]
    public void Minute_widget_counting_redraws_just_after_the_minutes_shown_drop()
    {
        var now = RaceStart - new TimeSpan(13, 4, 7, 30);

        var next = WidgetSchedule.NextMinuteRedrawUtc(SnapshotAt(now), now);

        Assert.Equal(RaceStart - new TimeSpan(13, 4, 7, 0) + TimeSpan.FromSeconds(1), next);
        Assert.Equal("04h 07m", SnapshotAt(next - TimeSpan.FromSeconds(1)).Detail);
        Assert.Equal("04h 06m", SnapshotAt(next).Detail);
    }

    [Fact]
    public void Minute_widget_race_day_redraws_each_minute_then_at_the_start()
    {
        var now = RaceStart - new TimeSpan(4, 12, 20);

        var next = WidgetSchedule.NextMinuteRedrawUtc(SnapshotAt(now), now);

        Assert.Equal(RaceStart - new TimeSpan(4, 12, 0) + TimeSpan.FromSeconds(1), next);
        Assert.Equal("04:11", SnapshotAt(next).Headline);

        var lastSeconds = RaceStart - TimeSpan.FromSeconds(20);
        Assert.Equal(RaceStart, WidgetSchedule.NextMinuteRedrawUtc(SnapshotAt(lastSeconds), lastSeconds));
    }

    [Fact]
    public void Minute_widget_counting_just_before_race_day_redraws_at_the_phase_change()
    {
        var now = RaceStart - TimeSpan.FromHours(24) - TimeSpan.FromSeconds(30);

        Assert.Equal(RaceStart - TimeSpan.FromHours(24), WidgetSchedule.NextMinuteRedrawUtc(SnapshotAt(now), now));
    }

    [Fact]
    public void Minute_widget_live_redraws_when_the_elapsed_minutes_change()
    {
        var now = RaceStart + new TimeSpan(1, 5, 40);

        var next = WidgetSchedule.NextMinuteRedrawUtc(SnapshotAt(now), now);

        Assert.Equal(RaceStart + new TimeSpan(1, 6, 0), next);
        Assert.Equal("Green flag 1:05 ago", SnapshotAt(next - TimeSpan.FromSeconds(1)).Detail);
        Assert.Equal("Green flag 1:06 ago", SnapshotAt(next).Detail);
    }

    [Fact]
    public void Minute_widget_awaiting_schedule_redraws_every_six_hours()
    {
        var now = RaceStart.AddDays(30);
        var snapshot = SnapshotAt(now, TestFeeds.Feed(TestFeeds.Tba(2027)));

        Assert.Equal(now + WidgetSchedule.AwaitingInterval, WidgetSchedule.NextMinuteRedrawUtc(snapshot, now));
    }

    [Fact]
    public void Alarm_within_a_minute_is_set_for_the_redraw_itself()
    {
        var now = RaceStart.AddDays(-3);

        Assert.Equal(now + TimeSpan.FromMinutes(1), WidgetSchedule.NextAlarmUtc(now + TimeSpan.FromMinutes(1), now));
        Assert.Equal(now + TimeSpan.FromSeconds(5), WidgetSchedule.NextAlarmUtc(now + TimeSpan.FromSeconds(5), now));
    }

    [Fact]
    public void Alarm_further_ahead_steps_a_quarter_of_the_way_with_a_30_second_minimum()
    {
        var now = RaceStart.AddDays(-3);

        Assert.Equal(now + TimeSpan.FromHours(6), WidgetSchedule.NextAlarmUtc(now + TimeSpan.FromHours(24), now));
        Assert.Equal(now + TimeSpan.FromSeconds(30), WidgetSchedule.NextAlarmUtc(now + TimeSpan.FromSeconds(90), now));
    }

    [Fact]
    public void Alarm_steps_reach_a_far_redraw_at_most_45_seconds_late_even_when_every_alarm_is_as_late_as_android_allows()
    {
        var redraw = RaceStart;
        var now = RaceStart - TimeSpan.FromHours(24);
        var alarms = 0;

        while (true)
        {
            var alarm = WidgetSchedule.NextAlarmUtc(redraw, now);
            alarms++;

            // Android may deliver an inexact alarm up to 75% of its lead time late (alarms under 10 s ahead are not delayed).
            var lead = alarm - now;
            var delivered = alarm + (lead < TimeSpan.FromSeconds(10) ? TimeSpan.Zero : lead * 0.75);

            if (alarm == redraw)
            {
                Assert.True(delivered - redraw <= TimeSpan.FromSeconds(45), $"Redraw {delivered - redraw} late");
                break;
            }

            Assert.True(delivered < redraw, "A step went off after the redraw was due");
            now = delivered;
        }

        Assert.InRange(alarms, 2, 40);
    }

    [Fact]
    public void Counting_segments_are_zero_padded_except_days()
    {
        var snapshot = SnapshotAt(RaceStart - new TimeSpan(3, 1, 5, 0));

        Assert.True(snapshot.ShowsSegments);
        Assert.Equal(("3", "01", "05"), (snapshot.DaysText, snapshot.HoursText, snapshot.MinutesText));
    }

    [Fact]
    public void Segments_are_only_shown_while_counting()
    {
        var snapshot = SnapshotAt(RaceStart - TimeSpan.FromHours(5));

        Assert.False(snapshot.ShowsSegments);
        Assert.Equal(("", "", ""), (snapshot.DaysText, snapshot.HoursText, snapshot.MinutesText));
    }

    [Theory]
    [InlineData(-13 * 24 - 4.5, "13d 04h 30m", "TO GO")]
    [InlineData(-30, "1d 06h 00m", "TO GO")]
    [InlineData(-5, "05:00", "RACE DAY")]
    [InlineData(1, "LIVE", "RACE UNDERWAY")]
    public void Compact_texts(double hoursFromStart, string headline, string caption)
    {
        var snapshot = SnapshotAt(RaceStart + TimeSpan.FromHours(hoursFromStart));

        Assert.Equal((headline, caption), (snapshot.CompactHeadline, snapshot.CompactCaption));
    }

    [Fact]
    public void Compact_texts_awaiting_schedule()
    {
        var snapshot = SnapshotAt(RaceStart.AddDays(30), TestFeeds.Feed(TestFeeds.Tba(2027)));

        Assert.Equal(("TBA", "BATHURST 1000"), (snapshot.CompactHeadline, snapshot.CompactCaption));
    }

    [Fact]
    public void Empty_feed_is_valid_and_awaits_a_schedule()
    {
        Assert.Empty(Core.Feed.FeedValidator.Validate(EventFeed.Empty));
        Assert.Equal(CountdownPhase.AwaitingSchedule, SnapshotAt(RaceStart, EventFeed.Empty).Phase);
    }
}
