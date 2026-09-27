using System.Globalization;

namespace RaceCountdown.Core;

/// <summary>Conversions between UTC instants and track wall-clock time.</summary>
public static class TrackTime
{
    public const string LocalFormat = "yyyy-MM-dd'T'HH:mm:ss";

    /// <summary>Finds a zone by IANA id. .NET uses ICU on Windows, so IANA ids work on every target.</summary>
    public static TimeZoneInfo FindZone(string ianaId) => TimeZoneInfo.FindSystemTimeZoneById(ianaId);

    public static string ToLocalString(DateTimeOffset instant, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTime(instant, zone).ToString(LocalFormat, CultureInfo.InvariantCulture);

    public static DateOnly ToLocalDate(DateTimeOffset instant, TimeZoneInfo zone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, zone).DateTime);

    public static bool TryParseLocal(string value, out DateTime local) =>
        DateTime.TryParseExact(value, LocalFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out local);
}
