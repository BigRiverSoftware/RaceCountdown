using Android.App;
using Android.Content;

namespace RaceCountdown.Widgets;

/// <summary>
/// Redraws the widgets after a reboot (alarms are cleared), a clock or time-zone change (the chronometer and alarm
/// times are computed from the wall clock), and an app update. All four broadcasts may be received by a manifest
/// receiver on current Android versions.
/// </summary>
[BroadcastReceiver(Name = "au.bigriversoftware.bathurstcountdown.WidgetSystemReceiver", Exported = true)]
[IntentFilter([Intent.ActionBootCompleted, Intent.ActionTimeChanged, Intent.ActionTimezoneChanged, Intent.ActionMyPackageReplaced])]
public class WidgetSystemReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is not null)
        {
            TimeZoneInfo.ClearCachedData();
            CountdownWidgetProvider.UpdateAll(context);
        }
    }
}
