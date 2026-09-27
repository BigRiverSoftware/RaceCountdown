using EventFeedBuilder;
using Json.Schema;
using RaceCountdown.Core.Feed;

// Builds events.json from supercars.com plus feed/overrides.json, checks it, and writes it to --out.
// Exit codes: 0 = written (or skipped outside race week), 1 = checks failed, 2 = bad arguments or fetch error.

var options = ParseArguments(args);
if (options is null)
{
    Console.Error.WriteLine("""
        Usage: EventFeedBuilder --out <dir> [--overrides feed/overrides.json] [--schema feed/events.schema.json]
                                [--previous <events.json>] [--source-dir <folder of saved pages>] [--race-week-only]
        """);
    return 2;
}

var now = TimeProvider.System.GetUtcNow();
var previous = options.PreviousPath is not null && File.Exists(options.PreviousPath)
    ? FeedSerializer.Deserialize(await File.ReadAllTextAsync(options.PreviousPath))
    : null;

if (options.RaceWeekOnly && (previous is null || !PublishChecks.IsRaceWeek(previous, now)))
{
    Console.WriteLine("Not race week; nothing to do.");
    return 0;
}

var overrides = FeedOverrides.Load(options.OverridesPath);
var schema = JsonSchema.FromText(await File.ReadAllTextAsync(options.SchemaPath));

using var http = HttpSupercarsSource.CreateClient();
ISupercarsSource source = options.SourceDirectory is not null
    ? new DirectorySupercarsSource(options.SourceDirectory)
    : new HttpSupercarsSource(http);

Dictionary<string, IReadOnlyList<SourceSession>> sessionsBySlug = [];
IReadOnlyList<CalendarEntry> calendar;
try
{
    calendar = SupercarsParser.ParseCalendar(await source.GetCalendarAsync(CancellationToken.None));
    if (calendar.Count == 0)
    {
        Console.Error.WriteLine("The calendar page had no events. The page layout has probably changed.");
        return 1;
    }

    foreach (var entry in FeedBuilder.BathurstEntries(calendar))
    {
        var page = await source.GetEventPageAsync(entry.Slug, CancellationToken.None);
        if (page is not null)
        {
            sessionsBySlug[entry.Slug] = SupercarsParser.ParseEventSessions(page);
        }

        Console.WriteLine($"{entry.Slug}: {(page is null ? "no event page" : $"{sessionsBySlug[entry.Slug].Count} sessions")}");
    }
}
catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or FormatException)
{
    Console.Error.WriteLine($"Could not read supercars.com: {ex.Message}");
    return 2;
}

var feed = FeedBuilder.Build(calendar, sessionsBySlug, overrides, now);
var json = FeedSerializer.Serialize(feed);
var overriddenIds = overrides.Events.Select(e => e.Id).ToHashSet();

var errors = PublishChecks.Run(json, feed, schema, previous, overriddenIds, now);
if (errors.Count > 0)
{
    Console.Error.WriteLine("The feed failed its checks and was not written:");
    foreach (var error in errors)
    {
        Console.Error.WriteLine($"  - {error}");
    }

    return 1;
}

Directory.CreateDirectory(options.OutputDirectory);
var outputPath = Path.Combine(options.OutputDirectory, "events.json");
await File.WriteAllTextAsync(outputPath, json);
Console.WriteLine($"Wrote {outputPath} with {feed.Events.Count} event(s).");
foreach (var ev in feed.Events)
{
    var race = ev.Sessions.FirstOrDefault();
    Console.WriteLine($"  {ev.Id}: {ev.Status}{(race is null ? string.Empty : $", {race.Name} at {race.StartLocal} track time")}");
}

return 0;

static BuilderOptions? ParseArguments(string[] args)
{
    string? output = null, previous = null, sourceDirectory = null;
    string overrides = Path.Combine("feed", "overrides.json");
    string schema = Path.Combine("feed", "events.schema.json");
    var raceWeekOnly = false;

    for (var i = 0; i < args.Length; i++)
    {
        string? Next() => i + 1 < args.Length ? args[++i] : null;

        switch (args[i])
        {
            case "--out": output = Next(); break;
            case "--overrides": overrides = Next() ?? overrides; break;
            case "--schema": schema = Next() ?? schema; break;
            case "--previous": previous = Next(); break;
            case "--source-dir": sourceDirectory = Next(); break;
            case "--race-week-only": raceWeekOnly = true; break;
            default: return null;
        }
    }

    return output is null ? null : new BuilderOptions(output, overrides, schema, previous, sourceDirectory, raceWeekOnly);
}

internal sealed record BuilderOptions(
    string OutputDirectory,
    string OverridesPath,
    string SchemaPath,
    string? PreviousPath,
    string? SourceDirectory,
    bool RaceWeekOnly);
