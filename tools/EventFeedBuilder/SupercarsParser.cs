using System.Globalization;
using System.Text.Json;

namespace EventFeedBuilder;

/// <summary>An event as listed on supercars.com/calendar.</summary>
public sealed record CalendarEntry(string Slug, string Title, string? Location, DateTimeOffset Start, DateTimeOffset End);

/// <summary>A session as listed on a supercars.com/events/{slug} page.</summary>
public sealed record SourceSession(string Name, string Type, string SeriesName, DateTimeOffset Start, DateTimeOffset? End);

/// <summary>Turns supercars.com pages into source records (field names from spike S1).</summary>
public static class SupercarsParser
{
    /// <summary>Events on the calendar page, one per slug, in date order.</summary>
    public static IReadOnlyList<CalendarEntry> ParseCalendar(string html) =>
        NextFlightData.ExtractObjects(html)
            .Where(o => HasString(o, "slug") && HasString(o, "startDate") && HasString(o, "endDate") && HasString(o, "title"))
            .Select(o => new CalendarEntry(
                o.GetProperty("slug").GetString()!,
                o.GetProperty("title").GetString()!,
                HasString(o, "location") ? o.GetProperty("location").GetString() : null,
                ParseInstant(o.GetProperty("startDate")),
                ParseInstant(o.GetProperty("endDate"))))
            .DistinctBy(e => e.Slug)
            .OrderBy(e => e.Start)
            .ToList();

    /// <summary>Every session on an event page, for all series, without duplicates.</summary>
    public static IReadOnlyList<SourceSession> ParseEventSessions(string html) =>
        NextFlightData.ExtractObjects(html)
            .Where(o => HasString(o, "name") && HasString(o, "type") && HasString(o, "startDate")
                        && o.TryGetProperty("series", out var series) && series.ValueKind == JsonValueKind.Object
                        && HasString(series, "name"))
            .Select(o => new SourceSession(
                o.GetProperty("name").GetString()!,
                o.GetProperty("type").GetString()!,
                o.GetProperty("series").GetProperty("name").GetString()!,
                ParseInstant(o.GetProperty("startDate")),
                HasString(o, "endDate") ? ParseInstant(o.GetProperty("endDate")) : null))
            .DistinctBy(s => (s.SeriesName, s.Name, s.Type, s.Start))
            .OrderBy(s => s.Start)
            .ToList();

    private static bool HasString(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String;

    // The site always includes the UTC offset ("2026-10-11T11:30:00.000+11:00"). Reject anything without one
    // rather than guess the zone.
    private static DateTimeOffset ParseInstant(JsonElement value)
    {
        var text = value.GetString()!;
        if (!DateTimeOffset.TryParseExact(text, "yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
            && !DateTimeOffset.TryParseExact(text, "yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
        {
            throw new FormatException($"Unexpected date format from supercars.com: '{text}'.");
        }

        return result;
    }
}
