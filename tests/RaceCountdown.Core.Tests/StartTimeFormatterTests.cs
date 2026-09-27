using System.Globalization;
using RaceCountdown.Core.Countdown;

namespace RaceCountdown.Core.Tests;

public class StartTimeFormatterTests
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
    private static readonly TimeZoneInfo Sydney = TrackTime.FindZone("Australia/Sydney");

    [Fact]
    public void Shows_only_track_time_when_the_user_is_in_the_same_zone()
    {
        var text = StartTimeFormatter.Format(TestFeeds.Race(TestFeeds.RaceStart2026), Sydney, Sydney, Culture);

        Assert.Equal("Sun 11 Oct, 11:30 AEDT", text);
    }

    [Fact]
    public void Shows_track_time_and_the_users_time()
    {
        var text = StartTimeFormatter.Format(TestFeeds.Race(TestFeeds.RaceStart2026), Sydney, TrackTime.FindZone("Australia/Perth"), Culture);

        Assert.Equal("Sun 11 Oct, 11:30 AEDT · 08:30 AWST (your time)", text);
    }

    [Fact]
    public void Shows_the_users_date_when_it_differs_from_the_track_date()
    {
        var text = StartTimeFormatter.Format(TestFeeds.Race(TestFeeds.RaceStart2026), Sydney, TrackTime.FindZone("America/New_York"), Culture);

        Assert.Equal("Sun 11 Oct, 11:30 AEDT · Sat 10 Oct, 20:30 UTC-4 (your time)", text);
    }

    [Fact]
    public void Uses_standard_time_before_the_october_daylight_saving_change()
    {
        // Sat 3 Oct 2026 is the day before NSW daylight saving starts.
        var session = TestFeeds.Race(new DateTimeOffset(2026, 10, 3, 1, 0, 0, TimeSpan.Zero));

        Assert.Equal("Sat 3 Oct, 11:00 AEST", StartTimeFormatter.Format(session, Sydney, Sydney, Culture));
    }

    [Fact]
    public void Queensland_differs_from_nsw_only_during_daylight_saving()
    {
        var brisbane = TrackTime.FindZone("Australia/Brisbane");

        Assert.Equal(
            "Sun 11 Oct, 11:30 AEDT · 10:30 AEST (your time)",
            StartTimeFormatter.Format(TestFeeds.Race(TestFeeds.RaceStart2026), Sydney, brisbane, Culture));
    }

    [Theory]
    [InlineData("Asia/Kolkata", "UTC+5:30")]
    [InlineData("Europe/London", "UTC+1")]
    [InlineData("Etc/UTC", "UTC")]
    [InlineData("Australia/Adelaide", "ACDT")]
    [InlineData("Pacific/Auckland", "NZDT")]
    public void Abbreviates_known_zones_and_falls_back_to_utc_offsets(string zoneId, string expected)
    {
        var zone = TrackTime.FindZone(zoneId);
        var at = TimeZoneInfo.ConvertTime(TestFeeds.RaceStart2026, zone);

        Assert.Equal(expected, StartTimeFormatter.Abbreviate(zone, at));
    }

    [Fact]
    public void Understands_windows_zone_ids()
    {
        // TimeZoneInfo.Local on Windows has a Windows id such as "W. Australia Standard Time".
        var perth = TimeZoneInfo.FindSystemTimeZoneById("W. Australia Standard Time");
        var at = TimeZoneInfo.ConvertTime(TestFeeds.RaceStart2026, perth);

        Assert.Equal("AWST", StartTimeFormatter.Abbreviate(perth, at));
    }
}
