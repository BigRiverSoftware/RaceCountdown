using RaceCountdown.Core.Feed;
using RaceCountdown.Core.Models;

namespace RaceCountdown.Core.Tests;

public class FeedValidatorTests
{
    [Fact]
    public void A_good_feed_has_no_errors()
    {
        Assert.Empty(FeedValidator.Validate(TestFeeds.Feed(TestFeeds.Bathurst2026(), TestFeeds.Tba(2027))));
    }

    [Fact]
    public void Rejects_an_unknown_schema_version()
    {
        var feed = TestFeeds.Feed(TestFeeds.Bathurst2026()) with { SchemaVersion = 2 };

        var error = Assert.Single(FeedValidator.Validate(feed));
        Assert.Contains("schemaVersion 2", error);
    }

    [Fact]
    public void Rejects_duplicate_ids()
    {
        var feed = TestFeeds.Feed(TestFeeds.Bathurst2026(), TestFeeds.Bathurst2026()) with
        {
            Series = [TestFeeds.Supercars, TestFeeds.Supercars],
            Tracks = [TestFeeds.MountPanorama, TestFeeds.MountPanorama],
        };

        var errors = FeedValidator.Validate(feed);

        Assert.Contains(errors, e => e.Contains("Duplicate series id"));
        Assert.Contains(errors, e => e.Contains("Duplicate track id"));
        Assert.Contains(errors, e => e.Contains("Duplicate event id"));
    }

    [Fact]
    public void Rejects_duplicate_session_ids()
    {
        var ev = TestFeeds.Bathurst2026();
        ev = ev with { Sessions = [ev.Sessions[0], ev.Sessions[0]] };

        Assert.Contains(FeedValidator.Validate(TestFeeds.Feed(ev)), e => e.Contains("Duplicate session"));
    }

    [Fact]
    public void Rejects_an_unknown_time_zone()
    {
        var feed = TestFeeds.Feed(TestFeeds.Bathurst2026()) with
        {
            Tracks = [TestFeeds.MountPanorama with { TimeZoneId = "Mars/Olympus_Mons" }],
        };

        Assert.Contains(FeedValidator.Validate(feed), e => e.Contains("unknown time zone"));
    }

    [Fact]
    public void Rejects_unknown_series_and_track()
    {
        var ev = TestFeeds.Bathurst2026() with { SeriesId = "indycar", TrackId = "laguna-seca" };

        var errors = FeedValidator.Validate(TestFeeds.Feed(ev));

        Assert.Contains(errors, e => e.Contains("unknown series 'indycar'"));
        Assert.Contains(errors, e => e.Contains("unknown track 'laguna-seca'"));
    }

    [Fact]
    public void Rejects_an_event_that_ends_before_it_starts()
    {
        var ev = TestFeeds.Bathurst2026() with { StartDate = new DateOnly(2026, 10, 12) };

        Assert.Contains(FeedValidator.Validate(TestFeeds.Feed(ev)), e => e.Contains("before it starts"));
    }

    [Fact]
    public void Rejects_a_confirmed_event_without_dates()
    {
        var ev = TestFeeds.Bathurst2026() with { StartDate = null };

        Assert.Contains(FeedValidator.Validate(TestFeeds.Feed(ev)), e => e.Contains("no start or end date"));
    }

    [Fact]
    public void Rejects_a_non_positive_duration()
    {
        var ev = TestFeeds.Bathurst2026();
        ev = ev with { Sessions = [ev.Sessions[0] with { EstimatedDuration = TimeSpan.Zero }] };

        Assert.Contains(FeedValidator.Validate(TestFeeds.Feed(ev)), e => e.Contains("non-positive"));
    }

    [Fact]
    public void Rejects_a_malformed_local_time()
    {
        var ev = TestFeeds.Bathurst2026();
        ev = ev with { Sessions = [ev.Sessions[0] with { StartLocal = "11 Oct 11:30am" }] };

        Assert.Contains(FeedValidator.Validate(TestFeeds.Feed(ev)), e => e.Contains("malformed startLocal"));
    }

    [Fact]
    public void Catches_a_start_time_parsed_with_the_standard_time_offset()
    {
        // NSW daylight saving starts on 4 Oct 2026, so race day is +11:00. A parser that assumed AEST (+10:00)
        // would publish 01:30Z; the cross-check against startLocal must catch it.
        var ev = TestFeeds.Bathurst2026();
        ev = ev with { Sessions = [ev.Sessions[0] with { StartUtc = new DateTimeOffset(2026, 10, 11, 1, 30, 0, TimeSpan.Zero) }] };

        var error = Assert.Single(FeedValidator.Validate(TestFeeds.Feed(ev)));
        Assert.Contains("2026-10-11T12:30:00 at the track", error);
    }

    [Fact]
    public void Rejects_a_session_outside_the_event_dates()
    {
        var ev = TestFeeds.Bathurst2026() with { EndDate = new DateOnly(2026, 10, 10) };

        Assert.Contains(FeedValidator.Validate(TestFeeds.Feed(ev)), e => e.Contains("outside the event dates"));
    }

    [Fact]
    public void Allows_a_tba_event_without_dates_or_sessions()
    {
        Assert.Empty(FeedValidator.Validate(TestFeeds.Feed(TestFeeds.Tba(2027))));
    }

    [Fact]
    public void Skips_the_local_time_check_when_the_track_is_unknown()
    {
        var ev = TestFeeds.Bathurst2026() with { TrackId = "nowhere" };

        var error = Assert.Single(FeedValidator.Validate(TestFeeds.Feed(ev)));
        Assert.Contains("unknown track", error);
    }

    [Fact]
    public void Cancelled_events_do_not_need_dates()
    {
        var ev = TestFeeds.Bathurst2026() with { Status = EventStatus.Cancelled, StartDate = null, EndDate = null };

        Assert.Empty(FeedValidator.Validate(TestFeeds.Feed(ev)));
    }
}
