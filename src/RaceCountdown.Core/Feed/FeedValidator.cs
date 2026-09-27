using RaceCountdown.Core.Models;

namespace RaceCountdown.Core.Feed;

/// <summary>
/// Sanity checks shared by the feed builder (before publishing) and the app (before trusting a download).
/// An empty result means the feed is valid.
/// </summary>
public static class FeedValidator
{
    public static IReadOnlyList<string> Validate(EventFeed feed)
    {
        var errors = new List<string>();

        if (feed.SchemaVersion != EventFeed.CurrentSchemaVersion)
        {
            errors.Add($"Unsupported schemaVersion {feed.SchemaVersion}; expected {EventFeed.CurrentSchemaVersion}.");
            return errors;
        }

        AddDuplicateIdErrors(errors, "series", feed.Series.Select(s => s.Id));
        AddDuplicateIdErrors(errors, "track", feed.Tracks.Select(t => t.Id));
        AddDuplicateIdErrors(errors, "event", feed.Events.Select(e => e.Id));

        var zones = new Dictionary<string, TimeZoneInfo>();
        foreach (var track in feed.Tracks)
        {
            try
            {
                zones[track.Id] = TrackTime.FindZone(track.TimeZoneId);
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                errors.Add($"Track '{track.Id}' has unknown time zone '{track.TimeZoneId}'.");
            }
        }

        foreach (var ev in feed.Events)
        {
            ValidateEvent(ev, feed, zones, errors);
        }

        return errors;
    }

    private static void ValidateEvent(RaceEvent ev, EventFeed feed, Dictionary<string, TimeZoneInfo> zones, List<string> errors)
    {
        var where = $"Event '{ev.Id}'";

        if (feed.FindSeries(ev.SeriesId) is null)
        {
            errors.Add($"{where} refers to unknown series '{ev.SeriesId}'.");
        }

        if (feed.FindTrack(ev.TrackId) is null)
        {
            errors.Add($"{where} refers to unknown track '{ev.TrackId}'.");
        }

        if (ev.StartDate is { } start && ev.EndDate is { } end && end < start)
        {
            errors.Add($"{where} ends ({end:yyyy-MM-dd}) before it starts ({start:yyyy-MM-dd}).");
        }

        if (ev.Status == EventStatus.Confirmed && (ev.StartDate is null || ev.EndDate is null))
        {
            errors.Add($"{where} is Confirmed but has no start or end date.");
        }

        AddDuplicateIdErrors(errors, $"session in {where}", ev.Sessions.Select(s => s.Id));

        foreach (var session in ev.Sessions)
        {
            var sessionWhere = $"{where} session '{session.Id}'";

            if (session.EstimatedDuration is { } duration && duration <= TimeSpan.Zero)
            {
                errors.Add($"{sessionWhere} has a non-positive estimatedDuration.");
            }

            if (!TrackTime.TryParseLocal(session.StartLocal, out var local))
            {
                errors.Add($"{sessionWhere} has malformed startLocal '{session.StartLocal}'.");
                continue;
            }

            if (!zones.TryGetValue(ev.TrackId, out var zone))
            {
                continue;
            }

            var expectedLocal = TrackTime.ToLocalString(session.StartUtc, zone);
            if (expectedLocal != session.StartLocal)
            {
                errors.Add($"{sessionWhere}: startUtc {session.StartUtc:O} is {expectedLocal} at the track, but startLocal says {session.StartLocal}.");
            }

            var localDate = DateOnly.FromDateTime(local);
            if ((ev.StartDate is { } s && localDate < s) || (ev.EndDate is { } e && localDate > e))
            {
                errors.Add($"{sessionWhere} on {localDate:yyyy-MM-dd} falls outside the event dates.");
            }
        }
    }

    private static void AddDuplicateIdErrors(List<string> errors, string kind, IEnumerable<string> ids)
    {
        foreach (var id in ids.GroupBy(i => i).Where(g => g.Count() > 1).Select(g => g.Key))
        {
            errors.Add($"Duplicate {kind} id '{id}'.");
        }
    }
}
