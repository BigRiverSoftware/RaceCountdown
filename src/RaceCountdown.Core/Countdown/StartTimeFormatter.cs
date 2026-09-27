using System.Globalization;
using RaceCountdown.Core.Models;

namespace RaceCountdown.Core.Countdown;

/// <summary>
/// Formats a start time in track time and the user's time,
/// e.g. "Sun 11 Oct, 11:30 AEDT · 08:30 AWST (your time)".
/// </summary>
public static class StartTimeFormatter
{
    private const string Separator = " · ";

    // .NET has no portable time-zone abbreviations, so the zones most users will be in are listed here.
    // Anything else falls back to "UTC+hh:mm".
    private static readonly Dictionary<string, (string Standard, string Daylight)> Abbreviations = new()
    {
        ["Australia/Sydney"] = ("AEST", "AEDT"),
        ["Australia/Melbourne"] = ("AEST", "AEDT"),
        ["Australia/Canberra"] = ("AEST", "AEDT"),
        ["Australia/Hobart"] = ("AEST", "AEDT"),
        ["Australia/Brisbane"] = ("AEST", "AEST"),
        ["Australia/Adelaide"] = ("ACST", "ACDT"),
        ["Australia/Broken_Hill"] = ("ACST", "ACDT"),
        ["Australia/Darwin"] = ("ACST", "ACST"),
        ["Australia/Perth"] = ("AWST", "AWST"),
        ["Pacific/Auckland"] = ("NZST", "NZDT"),
        ["Etc/UTC"] = ("UTC", "UTC"),
        ["UTC"] = ("UTC", "UTC"),
    };

    public static string Format(Session session, TimeZoneInfo trackZone, TimeZoneInfo userZone, CultureInfo culture)
    {
        var atTrack = TimeZoneInfo.ConvertTime(session.StartUtc, trackZone);
        var forUser = TimeZoneInfo.ConvertTime(session.StartUtc, userZone);

        var trackText = $"{FormatDate(atTrack, culture)}, {FormatTime(atTrack, culture)} {Abbreviate(trackZone, atTrack)}";

        if (atTrack.Offset == forUser.Offset)
        {
            return trackText;
        }

        var userTime = FormatTime(forUser, culture);
        var userText = forUser.Date == atTrack.Date
            ? $"{userTime} {Abbreviate(userZone, forUser)}"
            : $"{FormatDate(forUser, culture)}, {userTime} {Abbreviate(userZone, forUser)}";

        return $"{trackText}{Separator}{userText} (your time)";
    }

    public static string Abbreviate(TimeZoneInfo zone, DateTimeOffset at)
    {
        var id = zone.HasIanaId || !TimeZoneInfo.TryConvertWindowsIdToIanaId(zone.Id, out var iana) ? zone.Id : iana;

        if (Abbreviations.TryGetValue(id, out var names))
        {
            return zone.IsDaylightSavingTime(at) ? names.Daylight : names.Standard;
        }

        var offset = at.Offset;
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        offset = offset.Duration();
        return offset.Minutes == 0
            ? $"UTC{sign}{offset.Hours}"
            : $"UTC{sign}{offset.Hours}:{offset.Minutes:00}";
    }

    private static string FormatDate(DateTimeOffset value, CultureInfo culture) =>
        value.ToString("ddd d MMM", culture);

    private static string FormatTime(DateTimeOffset value, CultureInfo culture) =>
        value.ToString("t", culture);
}
