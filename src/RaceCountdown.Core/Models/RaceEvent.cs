namespace RaceCountdown.Core.Models;

/// <summary>
/// A race meeting. <see cref="StartDate"/> and <see cref="EndDate"/> are track-local dates and are null
/// when an event is expected but not yet on the calendar (<see cref="EventStatus.DateTba"/>).
/// </summary>
public sealed record RaceEvent(
    string Id,
    string SeriesId,
    string TrackId,
    string Name,
    DateOnly? StartDate,
    DateOnly? EndDate,
    EventStatus Status,
    IReadOnlyList<Session> Sessions);
