using System.Text.RegularExpressions;
using RaceCountdown.Core;
using RaceCountdown.Core.Models;

namespace EventFeedBuilder;

/// <summary>
/// Merges scraped supercars.com data with <see cref="FeedOverrides"/> into an <see cref="EventFeed"/>.
/// Precedence (plan §5.1): overrides, then the official event page.
/// </summary>
public static partial class FeedBuilder
{
    public const string SeriesId = "supercars";
    public const string TrackId = "mount-panorama";

    /// <summary>The main championship. Support categories on the same page have other series names (spike S1).</summary>
    public const string MainSeriesName = "Repco Supercars Championship";

    [GeneratedRegex(@"^(?<year>\d{4})-bathurst-1000$")]
    public static partial Regex BathurstSlug();

    [GeneratedRegex(@"^\d{4}\s+")]
    private static partial Regex LeadingYear();

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugCharacters();

    public static IEnumerable<CalendarEntry> BathurstEntries(IEnumerable<CalendarEntry> calendar) =>
        calendar.Where(e => BathurstSlug().IsMatch(e.Slug));

    /// <param name="sessionsBySlug">Sessions per event slug; a missing slug means the page did not exist.</param>
    public static EventFeed Build(
        IReadOnlyList<CalendarEntry> calendar,
        IReadOnlyDictionary<string, IReadOnlyList<SourceSession>> sessionsBySlug,
        FeedOverrides overrides,
        DateTimeOffset now)
    {
        var track = overrides.Tracks.FirstOrDefault(t => t.Id == TrackId)
            ?? throw new InvalidOperationException($"overrides.json must define track '{TrackId}'.");
        var zone = TrackTime.FindZone(track.TimeZoneId);

        var scraped = BathurstEntries(calendar)
            .Select(entry => BuildEvent(entry, sessionsBySlug.GetValueOrDefault(entry.Slug) ?? [], zone))
            .ToList();

        if (!scraped.Any(e => e.EndDate is null || e.EndDate >= TrackTime.ToLocalDate(now, zone)))
        {
            scraped.Add(Placeholder(scraped, now, zone));
        }

        var overrideIds = overrides.Events.Select(e => e.Id).ToHashSet();
        var events = scraped
            .Where(e => !overrideIds.Contains(e.Id))
            .Concat(overrides.Events)
            .OrderBy(e => e.StartDate ?? DateOnly.MaxValue)
            .ThenBy(e => e.Id, StringComparer.Ordinal)
            .ToList();

        return new EventFeed(EventFeed.CurrentSchemaVersion, now.ToUniversalTime(), overrides.Series, overrides.Tracks, events);
    }

    private static RaceEvent BuildEvent(CalendarEntry entry, IReadOnlyList<SourceSession> sourceSessions, TimeZoneInfo zone)
    {
        var sessions = sourceSessions
            .Where(s => s.SeriesName == MainSeriesName && s.Type == "Race")
            .Select(s => new Session(
                Slugify(s.Name),
                s.Name,
                SessionType.Race,
                s.Start.ToUniversalTime(),
                TrackTime.ToLocalString(s.Start, zone),
                s.End is { } end && end > s.Start ? end - s.Start : null))
            .ToList();

        return new RaceEvent(
            $"{SeriesId}-{entry.Slug}",
            SeriesId,
            TrackId,
            LeadingYear().Replace(entry.Title, string.Empty).Trim(),
            TrackTime.ToLocalDate(entry.Start, zone),
            TrackTime.ToLocalDate(entry.End, zone),
            // No main-race start time yet means the app shows "TBA" (plan D13), even if the dates are known.
            sessions.Count > 0 ? EventStatus.Confirmed : EventStatus.DateTba,
            sessions);
    }

    /// <summary>
    /// Between seasons the calendar may not list the next Bathurst 1000 at all. Publish a TBA event with no
    /// dates so the app says "TBA" rather than guessing (plan D13).
    /// </summary>
    private static RaceEvent Placeholder(IReadOnlyList<RaceEvent> scraped, DateTimeOffset now, TimeZoneInfo zone)
    {
        var lastYear = scraped.Select(e => e.StartDate?.Year).Max();
        var year = lastYear + 1 ?? TrackTime.ToLocalDate(now, zone).Year;
        return new RaceEvent($"{SeriesId}-{year}-bathurst-1000", SeriesId, TrackId, "Bathurst 1000", null, null, EventStatus.DateTba, []);
    }

    public static string Slugify(string name) =>
        NonSlugCharacters().Replace(name.ToLowerInvariant(), "-").Trim('-');
}
