namespace RaceCountdown.Core.Models;

/// <summary>A circuit. <see cref="TimeZoneId"/> is an IANA id such as "Australia/Sydney".</summary>
public sealed record Track(
    string Id,
    string Name,
    string Location,
    string CountryCode,
    string TimeZoneId,
    double Latitude,
    double Longitude);
