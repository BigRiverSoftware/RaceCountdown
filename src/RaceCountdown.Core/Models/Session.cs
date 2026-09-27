using System.Text.Json.Serialization;

namespace RaceCountdown.Core.Models;

/// <summary>
/// One on-track session. <see cref="StartUtc"/> is the instant all countdown maths uses;
/// <see cref="StartLocal"/> is the track wall-clock time ("yyyy-MM-ddTHH:mm:ss"), kept for display and for validation.
/// </summary>
public sealed record Session(
    string Id,
    string Name,
    SessionType Type,
    DateTimeOffset StartUtc,
    string StartLocal,
    TimeSpan? EstimatedDuration)
{
    /// <summary>Estimated finish; the start itself when no duration is known. Not part of the feed.</summary>
    [JsonIgnore]
    public DateTimeOffset EndUtc => StartUtc + (EstimatedDuration ?? TimeSpan.Zero);
}
