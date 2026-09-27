namespace RaceCountdown.Core.Models;

public sealed record EventFeed(
    int SchemaVersion,
    DateTimeOffset GeneratedUtc,
    IReadOnlyList<Series> Series,
    IReadOnlyList<Track> Tracks,
    IReadOnlyList<RaceEvent> Events)
{
    /// <summary>The feed format version this build of the app understands.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>A feed with nothing in it, for when no cached, bundled or downloaded feed is usable.</summary>
    public static EventFeed Empty { get; } = new(CurrentSchemaVersion, DateTimeOffset.MinValue, [], [], []);

    public Track? FindTrack(string id) => Tracks.FirstOrDefault(t => t.Id == id);

    public Series? FindSeries(string id) => Series.FirstOrDefault(s => s.Id == id);
}
