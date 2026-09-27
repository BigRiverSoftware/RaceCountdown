using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using RaceCountdown.Core.Models;

namespace RaceCountdown.Core.Feed;

/// <summary>What the app knows about its last feed downloads. Stored next to the cached feed.</summary>
/// <param name="ETag">ETag of the cached feed, for <c>If-None-Match</c>.</param>
/// <param name="LastSuccessUtc">When the server last gave a good answer (a new feed or "not modified").</param>
/// <param name="LastAttemptUtc">When a download was last tried, successful or not.</param>
/// <param name="LastRefreshFailed">True when the latest attempt failed, which is what makes an old feed "stale".</param>
public sealed record FeedCacheState(
    string? ETag = null,
    DateTimeOffset? LastSuccessUtc = null,
    DateTimeOffset? LastAttemptUtc = null,
    bool LastRefreshFailed = false)
{
    public static FeedCacheState Empty { get; } = new();
}

/// <summary>
/// The last validated feed, kept on the device (plan §5.3) in a folder the app and its widgets share.
/// Files are replaced atomically, so a widget reading at the same moment sees the old or the new file, never half of one.
/// A damaged file reads as missing.
/// </summary>
public sealed class FeedCache(string directory)
{
    public const string FeedFileName = "events.json";
    public const string StateFileName = "feed-state.json";

    private string FeedPath => Path.Combine(directory, FeedFileName);

    private string StatePath => Path.Combine(directory, StateFileName);

    public async Task<EventFeed?> LoadFeedAsync(CancellationToken cancellationToken = default)
    {
        var json = await ReadAsync(FeedPath, cancellationToken);
        return json is not null
               && FeedSerializer.TryDeserialize(json, out var feed)
               && FeedValidator.Validate(feed!).Count == 0
            ? feed
            : null;
    }

    public async Task<FeedCacheState> LoadStateAsync(CancellationToken cancellationToken = default)
    {
        var json = await ReadAsync(StatePath, cancellationToken);
        if (json is null)
        {
            return FeedCacheState.Empty;
        }

        try
        {
            return JsonSerializer.Deserialize(json, FeedCacheJsonContext.Default.FeedCacheState) ?? FeedCacheState.Empty;
        }
        catch (JsonException)
        {
            return FeedCacheState.Empty;
        }
    }

    public Task SaveFeedAsync(EventFeed feed, CancellationToken cancellationToken = default) =>
        WriteAsync(FeedPath, FeedSerializer.Serialize(feed), cancellationToken);

    public Task SaveStateAsync(FeedCacheState state, CancellationToken cancellationToken = default) =>
        WriteAsync(StatePath, JsonSerializer.Serialize(state, FeedCacheJsonContext.Default.FeedCacheState), cancellationToken);

    private static async Task<string?> ReadAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            return await File.ReadAllTextAsync(path, cancellationToken);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
    }

    private async Task WriteAsync(string path, string contents, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(directory);
        var temp = path + ".tmp";
        await File.WriteAllTextAsync(temp, contents, cancellationToken);
        File.Move(temp, path, overwrite: true);
    }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    Converters = [typeof(UtcDateTimeOffsetConverter)])]
[JsonSerializable(typeof(FeedCacheState))]
[ExcludeFromCodeCoverage]
internal sealed partial class FeedCacheJsonContext : JsonSerializerContext;
