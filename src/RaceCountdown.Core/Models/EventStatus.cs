namespace RaceCountdown.Core.Models;

public enum EventStatus
{
    /// <summary>Dates and session times are published.</summary>
    Confirmed,

    /// <summary>Dates are on the calendar but may still change.</summary>
    Provisional,

    /// <summary>The event is expected but its race start time is not yet published.</summary>
    DateTba,

    Cancelled,
}
