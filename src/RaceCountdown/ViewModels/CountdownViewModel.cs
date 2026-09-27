using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RaceCountdown.Core.Countdown;
using RaceCountdown.Core.Feed;
using RaceCountdown.Core.Models;
using RaceCountdown.Services;

namespace RaceCountdown.ViewModels;

/// <summary>
/// Drives <see cref="Views.CountdownPage"/>: ticks once a second while visible (plan §7) and refreshes the feed on
/// start-up, on demand, and whenever <see cref="RefreshPolicy"/> says so.
/// </summary>
public sealed partial class CountdownViewModel : ObservableObject
{
    private readonly FeedStore store;
    private readonly TimeProvider time;
    private readonly IWidgetUpdater widgets;
    private readonly ILogger<CountdownViewModel> logger;
    private readonly IDispatcherTimer timer;
    private readonly EventFilter filter = EventFilter.Bathurst1000MainRace;
    private Task? loading;

    // RefreshView sets IsRefreshing itself before running the command, so it can't double as the in-flight flag.
    private bool refreshInFlight;

    public CountdownViewModel(FeedStore store, TimeProvider time, IWidgetUpdater widgets, IDispatcher dispatcher, ILogger<CountdownViewModel> logger)
    {
        this.store = store;
        this.time = time;
        this.widgets = widgets;
        this.logger = logger;
        Display = Build();

        timer = dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Tick += (_, _) => Tick();
    }

    [ObservableProperty]
    public partial CountdownDisplay Display { get; private set; }

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    /// <summary>Called when the page appears or the app returns to the foreground. Safe to call repeatedly.</summary>
    public async Task StartAsync()
    {
        // The user may have changed time zone while the app was in the background.
        TimeZoneInfo.ClearCachedData();

        loading ??= LoadThenRefreshAsync();
        await loading;
        Tick();
        timer.Start();
    }

    /// <summary>Called when the page disappears or the app goes to the background.</summary>
    public void Stop() => timer.Stop();

    [RelayCommand]
    private Task RefreshAsync() => RefreshCoreAsync();

    private async Task LoadThenRefreshAsync()
    {
        await store.LoadAsync();
        Display = Build();

        // Always check once at start-up (plan §5.3); the cached or bundled feed is already on screen.
        _ = RefreshCoreAsync();
    }

    private void Tick()
    {
        Display = Build();

        if (loading is { IsCompleted: true } && RefreshPolicy.IsDue(store.Feed, filter, store.State, time.GetUtcNow()))
        {
            _ = RefreshCoreAsync();
        }
    }

    private CountdownDisplay Build()
    {
        var state = CountdownCalculator.Calculate(store.Feed ?? EventFeed.Empty, filter, time, store.State.LastRefreshFailed);
        return CountdownDisplay.Create(state, store.Feed, store.State, TimeZoneInfo.Local, CultureInfo.CurrentCulture);
    }

    private async Task RefreshCoreAsync()
    {
        if (refreshInFlight)
        {
            IsRefreshing = true;
            return;
        }

        refreshInFlight = true;
        IsRefreshing = true;
        try
        {
            var status = await store.RefreshAsync();
            logger.LogInformation("Feed refresh: {Status}", status);
            if (status == FeedFetchStatus.Updated)
            {
                widgets.RequestUpdate();
            }
        }
        catch (Exception ex)
        {
            // FeedStore reports network problems as a status; anything else (e.g. a full disk) must not crash the app.
            logger.LogError(ex, "Feed refresh failed");
        }
        finally
        {
            refreshInFlight = false;
            IsRefreshing = false;
            Display = Build();
        }
    }
}
