using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using RaceCountdown.Core.Models;

namespace RaceCountdown.Core.Feed;

public enum FeedFetchStatus
{
    /// <summary>A new, valid feed was downloaded.</summary>
    Updated,

    /// <summary>The server says the cached copy (matched by ETag) is still current.</summary>
    NotModified,

    /// <summary>Network error, timeout, bad status, or a feed that failed the checks. Keep using the cache.</summary>
    Failed,
}

public sealed record FeedFetchResult(FeedFetchStatus Status, EventFeed? Feed = null, string? ETag = null, string? Error = null)
{
    public static FeedFetchResult NotModified { get; } = new(FeedFetchStatus.NotModified);

    public static FeedFetchResult Fail(string error) => new(FeedFetchStatus.Failed, Error: error);
}

public interface IEventFeedClient
{
    /// <param name="etag">The ETag of the cached feed, sent as <c>If-None-Match</c>; null when nothing is cached.</param>
    Task<FeedFetchResult> FetchAsync(string? etag, CancellationToken cancellationToken = default);
}

/// <summary>
/// Downloads the published feed (plan §5.3) and checks it again on the device. Never throws for network or
/// data problems: they come back as <see cref="FeedFetchStatus.Failed"/> so the caller keeps its cached feed.
/// </summary>
public sealed class EventFeedClient(HttpClient http, Uri feedUrl) : IEventFeedClient
{
    public static readonly Uri DefaultFeedUrl = new("https://bigriversoftware.github.io/RaceCountdown/events.json");

    public async Task<FeedFetchResult> FetchAsync(string? etag, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, feedUrl);
        if (etag is not null && EntityTagHeaderValue.TryParse(etag, out var tag))
        {
            request.Headers.IfNoneMatch.Add(tag);
        }

        try
        {
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                return FeedFetchResult.NotModified;
            }

            if (!response.IsSuccessStatusCode)
            {
                return FeedFetchResult.Fail($"The feed server returned {(int)response.StatusCode} {response.ReasonPhrase}.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var feed = await FeedSerializer.DeserializeAsync(stream, cancellationToken);

            var errors = FeedValidator.Validate(feed);
            if (errors.Count > 0)
            {
                return FeedFetchResult.Fail($"The feed failed its checks: {errors[0]}");
            }

            return new FeedFetchResult(FeedFetchStatus.Updated, feed, response.Headers.ETag?.ToString());
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient.Timeout surfaces as a cancellation that the caller did not ask for.
            return FeedFetchResult.Fail("The feed download timed out.");
        }
        catch (HttpRequestException ex)
        {
            return FeedFetchResult.Fail($"The feed could not be downloaded: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return FeedFetchResult.Fail($"The feed is not valid JSON: {ex.Message}");
        }
    }
}
