using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace RaceCountdown.Core.Widgets;

/// <summary>
/// The Adaptive Card data for the Windows widget (plan §8.3): the <see cref="WidgetSnapshot"/> texts under the names
/// the card templates bind to (<c>${headline}</c> and so on). The widget is redrawn each minute, so it uses the
/// minute-precision <see cref="WidgetSnapshot.Detail"/>.
/// </summary>
public static class WidgetCardData
{
    /// <summary>Shown under the countdown when the feed is stale, as on the Android widget.</summary>
    public const string StaleText = "Offline · may be out of date";

    private static readonly JsonWriterOptions Options = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <param name="backgroundUrl">The card background (a data URI), or null for none.</param>
    public static string ToJson(WidgetSnapshot snapshot, string? backgroundUrl)
    {
        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer, Options))
        {
            json.WriteStartObject();
            json.WriteString("title", snapshot.Title);
            json.WriteString("headline", snapshot.Headline);
            json.WriteString("detail", snapshot.Detail);
            json.WriteBoolean("showSegments", snapshot.ShowsSegments);
            json.WriteString("days", snapshot.DaysText);
            json.WriteString("hours", snapshot.HoursText);
            json.WriteString("minutes", snapshot.MinutesText);
            json.WriteString("compactHeadline", snapshot.CompactHeadline);
            json.WriteString("compactCaption", snapshot.CompactCaption);
            json.WriteString("accessibleText", snapshot.AccessibleText);
            json.WriteBoolean("isStale", snapshot.IsStale);
            json.WriteString("staleText", StaleText);
            json.WriteString("background", backgroundUrl ?? "");
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}
