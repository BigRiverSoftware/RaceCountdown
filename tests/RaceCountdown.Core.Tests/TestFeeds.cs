using RaceCountdown.Core.Models;

namespace RaceCountdown.Core.Tests;

/// <summary>Builders for in-memory feeds. The defaults are the real 2026 Bathurst 1000.</summary>
internal static class TestFeeds
{
    public static readonly Series Supercars = new("supercars", "Repco Supercars Championship", "Supercars", "#E10600");

    public static readonly Track MountPanorama = new(
        "mount-panorama", "Mount Panorama Circuit", "Bathurst, NSW", "AU", "Australia/Sydney", -33.4475, 149.557);

    /// <summary>2026-10-11 11:30 AEDT.</summary>
    public static readonly DateTimeOffset RaceStart2026 = new(2026, 10, 11, 0, 30, 0, TimeSpan.Zero);

    public static Session Race(DateTimeOffset startUtc, string id = "race-30", TimeSpan? duration = null, SessionType type = SessionType.Race)
    {
        var zone = TrackTime.FindZone(MountPanorama.TimeZoneId);
        return new Session(id, id, type, startUtc, TrackTime.ToLocalString(startUtc, zone), duration ?? TimeSpan.FromHours(7));
    }

    public static RaceEvent Bathurst(
        int year,
        DateTimeOffset raceStartUtc,
        EventStatus status = EventStatus.Confirmed,
        string name = "Repco Bathurst 1000")
    {
        var raceDay = TrackTime.ToLocalDate(raceStartUtc, TrackTime.FindZone(MountPanorama.TimeZoneId));
        return new RaceEvent(
            $"supercars-{year}-bathurst-1000", "supercars", "mount-panorama", name,
            raceDay.AddDays(-3), raceDay, status, [Race(raceStartUtc)]);
    }

    public static RaceEvent Bathurst2026() => Bathurst(2026, RaceStart2026);

    public static RaceEvent Tba(int year, DateOnly? start = null, DateOnly? end = null) =>
        new($"supercars-{year}-bathurst-1000", "supercars", "mount-panorama", "Bathurst 1000", start, end, EventStatus.DateTba, []);

    public static EventFeed Feed(params RaceEvent[] events) => Feed(new DateTimeOffset(2026, 9, 27, 0, 0, 0, TimeSpan.Zero), events);

    public static EventFeed Feed(DateTimeOffset generatedUtc, params RaceEvent[] events) =>
        new(EventFeed.CurrentSchemaVersion, generatedUtc, [Supercars], [MountPanorama], events);
}
