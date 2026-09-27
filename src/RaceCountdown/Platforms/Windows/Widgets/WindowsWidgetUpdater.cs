using System.Diagnostics;
using Microsoft.Windows.Widgets.Providers;
using RaceCountdown.Core.Feed;
using RaceCountdown.Services;

namespace RaceCountdown.Widgets;

/// <summary>
/// Redraws the Widgets Board widgets from the app process after the app downloads a new feed, so they update even
/// while the board isn't showing. On Windows 10, which has no Widgets Board, this does nothing.
/// </summary>
public sealed class WindowsWidgetUpdater(FeedStore store) : IWidgetUpdater
{
    public void RequestUpdate()
    {
        try
        {
            var manager = WidgetManager.GetDefault();
            var infos = manager.GetWidgetInfos();
            if (infos is null || infos.Length == 0)
            {
                return;
            }

            var snapshot = WindowsFeed.Snapshot(store, DateTimeOffset.UtcNow);
            foreach (var info in infos)
            {
                manager.UpdateWidget(WidgetCard.Build(info.WidgetContext.Id, info.WidgetContext.Size, snapshot));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Widget update from the app failed: {ex}");
        }
    }
}
