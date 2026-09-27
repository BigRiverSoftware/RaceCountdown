using System.Text.Json;
using Json.Schema;
using RaceCountdown.Core.Countdown;
using RaceCountdown.Core.Feed;
using RaceCountdown.Core.Models;

namespace EventFeedBuilder;

/// <summary>
/// The gate a feed must pass before it is published (plan §5.1 step 4). If anything fails, nothing is
/// published and the previous good feed stays live.
/// </summary>
public static class PublishChecks
{
    /// <summary>A start time may not move further than this between publishes unless the event is overridden.</summary>
    public static readonly TimeSpan MaxUnreviewedMove = TimeSpan.FromDays(7);

    public static IReadOnlyList<string> Run(
        string json,
        EventFeed feed,
        JsonSchema schema,
        EventFeed? previous,
        IReadOnlySet<string> overriddenEventIds,
        DateTimeOffset now)
    {
        var errors = new List<string>();
        errors.AddRange(CheckSchema(json, schema));
        errors.AddRange(FeedValidator.Validate(feed));

        if (previous is not null)
        {
            errors.AddRange(CheckMoves(feed, previous, overriddenEventIds));
        }

        var state = CountdownCalculator.Calculate(feed, EventFilter.Bathurst1000MainRace, now);
        if (state.Phase == CountdownPhase.AwaitingSchedule && state.Event is null)
        {
            errors.Add("There is no future Bathurst 1000 main race and no upcoming event marked DateTba.");
        }

        return errors;
    }

    public static IEnumerable<string> CheckSchema(string json, JsonSchema schema)
    {
        using var document = JsonDocument.Parse(json);
        var results = schema.Evaluate(document.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
        if (results.IsValid)
        {
            return [];
        }

        return (results.Details ?? [])
            .SelectMany(detail => (detail.Errors ?? new Dictionary<string, string>())
                .Select(error => $"Schema: {detail.InstanceLocation} ({error.Key}): {error.Value}"))
            .DefaultIfEmpty("Schema: the feed does not match events.schema.json.")
            .ToList();
    }

    /// <summary>Plan §5.1: guards against a bad parse moving a race by weeks without anyone noticing.</summary>
    public static IEnumerable<string> CheckMoves(EventFeed feed, EventFeed previous, IReadOnlySet<string> overriddenEventIds)
    {
        var before = previous.Events
            .SelectMany(e => e.Sessions.Select(s => (Key: (e.Id, s.Id), s.StartUtc)))
            .ToDictionary(x => x.Key, x => x.StartUtc);

        foreach (var ev in feed.Events.Where(e => !overriddenEventIds.Contains(e.Id)))
        {
            foreach (var session in ev.Sessions)
            {
                if (before.TryGetValue((ev.Id, session.Id), out var oldStart)
                    && (session.StartUtc - oldStart).Duration() > MaxUnreviewedMove)
                {
                    yield return $"Event '{ev.Id}' session '{session.Id}' moved from {oldStart:O} to {session.StartUtc:O}. "
                        + "Moves over 7 days need an entry in overrides.json.";
                }
            }
        }
    }

    /// <summary>
    /// True within a week either side of a main race in the given feed. The 6-hourly scheduled runs only
    /// publish in race week (plan §5.1 step 6).
    /// </summary>
    public static bool IsRaceWeek(EventFeed feed, DateTimeOffset now) =>
        feed.Events
            .Where(EventFilter.Bathurst1000MainRace.MatchesEvent)
            .SelectMany(e => e.Sessions.Where(EventFilter.Bathurst1000MainRace.MatchesSession))
            .Any(s => (s.StartUtc - now).Duration() <= TimeSpan.FromDays(7));
}
