using Android.App;
using Android.Appwidget;
using Android.Content;

namespace RaceCountdown.Widgets;

/// <summary>
/// The 1×1 countdown widget: its own entry in the widget picker, drawn from <see cref="Core.Widgets.TinyWidgetContent"/>.
/// Everything else is inherited, so it is redrawn and scheduled together with <see cref="CountdownWidgetProvider"/>
/// (one alarm for all widgets).
/// </summary>
[BroadcastReceiver(Name = "au.bigriversoftware.bathurstcountdown.TinyCountdownWidgetProvider", Label = "Bathurst Countdown (1×1)", Exported = true)]
[IntentFilter([AppWidgetManager.ActionAppwidgetUpdate])]
[MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/countdown_widget_tiny_info")]
public class TinyCountdownWidgetProvider : CountdownWidgetProvider
{
}
