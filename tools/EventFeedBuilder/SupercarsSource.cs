using System.Net;
using System.Reflection;

namespace EventFeedBuilder;

/// <summary>Where the supercars.com pages come from: the live site, or saved copies for offline runs and tests.</summary>
public interface ISupercarsSource
{
    Task<string> GetCalendarAsync(CancellationToken cancellationToken);

    /// <summary>The event page, or null when it does not exist yet.</summary>
    Task<string?> GetEventPageAsync(string slug, CancellationToken cancellationToken);
}

/// <summary>
/// Fetches the public pages that robots.txt allows (/calendar and /events/*), never /api/ (spike S1),
/// with an honest User-Agent and a pause between requests.
/// </summary>
public sealed class HttpSupercarsSource(HttpClient http) : ISupercarsSource
{
    public static readonly Uri BaseAddress = new("https://www.supercars.com/");

    private static readonly TimeSpan PauseBetweenRequests = TimeSpan.FromSeconds(2);

    private bool _hasRequested;

    public static HttpClient CreateClient()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
        var http = new HttpClient { BaseAddress = BaseAddress, Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"BathurstCountdownFeedBuilder/{version} (+https://bigriversoftware.au)");
        return http;
    }

    public async Task<string> GetCalendarAsync(CancellationToken cancellationToken) =>
        await GetAsync("calendar", cancellationToken)
            ?? throw new InvalidOperationException("supercars.com/calendar returned 404.");

    public Task<string?> GetEventPageAsync(string slug, CancellationToken cancellationToken) =>
        GetAsync($"events/{Uri.EscapeDataString(slug)}", cancellationToken);

    private async Task<string?> GetAsync(string path, CancellationToken cancellationToken)
    {
        if (_hasRequested)
        {
            await Task.Delay(PauseBetweenRequests, cancellationToken);
        }

        _hasRequested = true;
        using var response = await http.GetAsync(path, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}

/// <summary>Reads <c>calendar.html</c> and <c>{slug}.html</c> from a folder.</summary>
public sealed class DirectorySupercarsSource(string directory) : ISupercarsSource
{
    public Task<string> GetCalendarAsync(CancellationToken cancellationToken) =>
        File.ReadAllTextAsync(Path.Combine(directory, "calendar.html"), cancellationToken);

    public async Task<string?> GetEventPageAsync(string slug, CancellationToken cancellationToken)
    {
        var path = Path.Combine(directory, $"{slug}.html");
        return File.Exists(path) ? await File.ReadAllTextAsync(path, cancellationToken) : null;
    }
}
