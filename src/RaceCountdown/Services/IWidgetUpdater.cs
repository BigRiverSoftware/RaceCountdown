namespace RaceCountdown.Services;

/// <summary>Asks the platform's home-screen widgets to redraw from the latest feed (plan §8).</summary>
public interface IWidgetUpdater
{
    void RequestUpdate();
}

/// <summary>Used on platforms without a widget implementation.</summary>
public sealed class NoWidgetUpdater : IWidgetUpdater
{
    public void RequestUpdate()
    {
    }
}
