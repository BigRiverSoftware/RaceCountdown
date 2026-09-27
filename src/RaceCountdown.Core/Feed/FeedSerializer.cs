using System.Text.Json;
using RaceCountdown.Core.Models;

namespace RaceCountdown.Core.Feed;

public static class FeedSerializer
{
    public static string Serialize(EventFeed feed) =>
        JsonSerializer.Serialize(feed, FeedJsonContext.Default.EventFeed);

    /// <exception cref="JsonException">The text is not a well-formed feed.</exception>
    public static EventFeed Deserialize(string json) =>
        JsonSerializer.Deserialize(json, FeedJsonContext.Default.EventFeed)
            ?? throw new JsonException("The feed is empty.");

    public static async Task<EventFeed> DeserializeAsync(Stream stream, CancellationToken cancellationToken = default) =>
        await JsonSerializer.DeserializeAsync(stream, FeedJsonContext.Default.EventFeed, cancellationToken)
            ?? throw new JsonException("The feed is empty.");

    /// <summary>Returns false instead of throwing when the text is not a well-formed feed.</summary>
    public static bool TryDeserialize(string json, out EventFeed? feed)
    {
        try
        {
            feed = Deserialize(json);
            return true;
        }
        catch (JsonException)
        {
            feed = null;
            return false;
        }
    }
}
