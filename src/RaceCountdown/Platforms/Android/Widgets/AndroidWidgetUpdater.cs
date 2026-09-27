using RaceCountdown.Services;

namespace RaceCountdown.Widgets;

/// <summary>Redraws the Android home-screen widgets after the app downloads a new feed.</summary>
public sealed class AndroidWidgetUpdater : IWidgetUpdater
{
    public void RequestUpdate() => CountdownWidgetProvider.UpdateAll(Platform.AppContext);
}
