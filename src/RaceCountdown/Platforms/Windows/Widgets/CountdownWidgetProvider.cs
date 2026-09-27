using System.Diagnostics;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Providers;
using RaceCountdown.Core.Countdown;
using RaceCountdown.Core.Feed;
using RaceCountdown.Core.Widgets;
using Windows.ApplicationModel;

namespace RaceCountdown.Widgets;

/// <summary>
/// The Widgets Board countdown (plan §8.3). Draws <see cref="WidgetSnapshot"/> through <see cref="WidgetCard"/> and holds
/// no countdown logic of its own. The board cannot tick a countdown, so while any widget is on screen (between
/// <see cref="Activate"/> and <see cref="Deactivate"/>) it is redrawn whenever the minutes shown change.
/// All widget instances show the same race, so one timer serves them all.
/// </summary>
internal sealed partial class CountdownWidgetProvider : IWidgetProvider
{
    /// <summary>The widget definition id in <c>Package.appxmanifest</c> (plan §10).</summary>
    public const string DefinitionId = "BathurstCountdown.Countdown";

    /// <summary>The <c>Action.Execute</c> verb sent when the card is clicked.</summary>
    public const string OpenVerb = "open";

    private readonly object sync = new();
    private readonly Dictionary<string, WidgetState> widgets = [];
    private readonly SemaphoreSlim redrawGate = new(1, 1);
    private readonly FeedStore store = WindowsFeed.CreateStore();
    private readonly Timer timer;
    private readonly EventWaitHandle noWidgetsLeft;
    private int refreshing;

    public CountdownWidgetProvider(EventWaitHandle noWidgetsLeft)
    {
        this.noWidgetsLeft = noWidgetsLeft;
        timer = new Timer(_ => _ = RedrawAsync(), null, Timeout.Infinite, Timeout.Infinite);

        // After a reboot, a crash or an app update, the board restarts the provider with widgets already pinned.
        // GetWidgetInfos returns null, not an empty array, when none are.
        foreach (var info in WidgetManager.GetDefault().GetWidgetInfos() ?? [])
        {
            var context = info.WidgetContext;
            widgets[context.Id] = new WidgetState(context.Size, context.IsActive);
        }

        _ = RedrawAsync();
    }

    public void CreateWidget(WidgetContext widgetContext)
    {
        lock (sync)
        {
            widgets[widgetContext.Id] = new WidgetState(widgetContext.Size, widgetContext.IsActive);
        }

        _ = RedrawAsync();
    }

    public void DeleteWidget(string widgetId, string customState)
    {
        bool empty;
        lock (sync)
        {
            widgets.Remove(widgetId);
            empty = widgets.Count == 0;
        }

        if (empty)
        {
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            noWidgetsLeft.Set();
        }
    }

    public void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        if (actionInvokedArgs.Verb == OpenVerb)
        {
            _ = LaunchAppAsync();
        }
    }

    public void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
    {
        // Resized: each size has its own template.
        var context = contextChangedArgs.WidgetContext;
        lock (sync)
        {
            if (widgets.TryGetValue(context.Id, out var state))
            {
                widgets[context.Id] = state with { Size = context.Size };
            }
        }

        _ = RedrawAsync();
    }

    public void Activate(WidgetContext widgetContext) => SetActive(widgetContext.Id, true);

    public void Deactivate(string widgetId) => SetActive(widgetId, false);

    private void SetActive(string widgetId, bool isActive)
    {
        lock (sync)
        {
            if (widgets.TryGetValue(widgetId, out var state))
            {
                widgets[widgetId] = state with { IsActive = isActive };
            }
        }

        // Redrawing on deactivate too is cheap, and stops the timer once nothing is on screen.
        _ = RedrawAsync();
    }

    private async Task RedrawAsync()
    {
        await redrawGate.WaitAsync();
        try
        {
            (string Id, WidgetSize Size)[] targets;
            bool anyActive;
            lock (sync)
            {
                targets = widgets.Select(w => (w.Key, w.Value.Size)).ToArray();
                anyActive = widgets.Values.Any(w => w.IsActive);
            }

            if (targets.Length == 0)
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                return;
            }

            // Reload every time: the app may have downloaded a newer feed since the last redraw.
            await store.LoadAsync();
            var now = DateTimeOffset.UtcNow;
            var snapshot = WindowsFeed.Snapshot(store, now);

            var manager = WidgetManager.GetDefault();
            foreach (var (id, size) in targets)
            {
                manager.UpdateWidget(WidgetCard.Build(id, size, snapshot));
            }

            timer.Change(anyActive ? DelayUntil(WidgetSchedule.NextMinuteRedrawUtc(snapshot, now), now) : Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            if (RefreshPolicy.IsDue(store.Feed, EventFilter.Bathurst1000MainRace, store.State, now))
            {
                StartRefresh();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Widget redraw failed: {ex}");
        }
        finally
        {
            redrawGate.Release();
        }
    }

    // One download at a time; RefreshPolicy limits retries after a failure to one per 15 minutes.
    private void StartRefresh()
    {
        if (Interlocked.Exchange(ref refreshing, 1) == 1)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var status = await store.RefreshAsync();
                Debug.WriteLine($"Widget feed refresh: {status}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Widget feed refresh failed: {ex}");
            }
            finally
            {
                Volatile.Write(ref refreshing, 0);
            }

            await RedrawAsync();
        });
    }

    private static TimeSpan DelayUntil(DateTimeOffset whenUtc, DateTimeOffset now)
    {
        var delay = whenUtc - now;
        return delay < TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(1) : delay;
    }

    private static async Task LaunchAppAsync()
    {
        try
        {
            var entries = await Package.Current.GetAppListEntriesAsync();
            await entries[0].LaunchAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Opening the app from the widget failed: {ex}");
        }
    }

    private readonly record struct WidgetState(WidgetSize Size, bool IsActive);
}
