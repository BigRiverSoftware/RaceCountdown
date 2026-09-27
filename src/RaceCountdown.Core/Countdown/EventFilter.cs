using RaceCountdown.Core.Models;

namespace RaceCountdown.Core.Countdown;

/// <summary>
/// Chooses which sessions a countdown follows. Null criteria match anything, so later versions can
/// follow a whole series or track. v1 uses <see cref="Bathurst1000MainRace"/> (plan D12).
/// </summary>
public sealed record EventFilter(
    string? SeriesId = null,
    string? TrackId = null,
    string? EventNameContains = null,
    SessionType? SessionType = null)
{
    public static EventFilter Bathurst1000MainRace { get; } =
        new("supercars", "mount-panorama", "Bathurst 1000", Models.SessionType.Race);

    public bool MatchesEvent(RaceEvent ev) =>
        (SeriesId is null || ev.SeriesId == SeriesId)
        && (TrackId is null || ev.TrackId == TrackId)
        && (EventNameContains is null || ev.Name.Contains(EventNameContains, StringComparison.OrdinalIgnoreCase));

    public bool MatchesSession(Session session) =>
        SessionType is null || session.Type == SessionType;
}
