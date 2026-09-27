namespace RaceCountdown.Core.Models;

/// <summary>A racing series, e.g. "supercars".</summary>
public sealed record Series(string Id, string Name, string? ShortName, string AccentColour);
