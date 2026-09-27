using System.Text.Json;
using System.Text.Json.Serialization;
using RaceCountdown.Core.Feed;
using RaceCountdown.Core.Models;

namespace EventFeedBuilder;

/// <summary>
/// The hand-curated <c>feed/overrides.json</c>. It holds the reference data for series and tracks (the
/// scraped pages have no time zones or coordinates) and any events that must replace the scraped ones.
/// An override event replaces the scraped event with the same id completely, or is added if there is none.
/// </summary>
public sealed record FeedOverrides(
    IReadOnlyList<Series> Series,
    IReadOnlyList<Track> Tracks,
    IReadOnlyList<RaceEvent> Events)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(), new UtcDateTimeOffsetConverter() },
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static FeedOverrides Parse(string json) =>
        JsonSerializer.Deserialize<FeedOverrides>(json, Options)
            ?? throw new JsonException("overrides.json is empty.");

    public static FeedOverrides Load(string path) => Parse(File.ReadAllText(path));
}
