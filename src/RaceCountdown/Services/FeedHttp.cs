using System.Net.Http.Headers;

namespace RaceCountdown.Services;

/// <summary>HTTP settings for feed downloads, shared by the app and the widgets, which run without the MAUI UI.</summary>
public static class FeedHttp
{
    public static void Configure(HttpClient http)
    {
        http.Timeout = TimeSpan.FromSeconds(20);
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("BathurstCountdown", AppInfo.Current.VersionString));
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(+https://bigriversoftware.au)"));
    }

    /// <summary>A client for code outside the app's dependency injection (the widgets).</summary>
    public static HttpClient Create()
    {
        var http = new HttpClient();
        Configure(http);
        return http;
    }
}
