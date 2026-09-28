using Android.App;
using Android.Appwidget;
using Android.Content;

namespace RaceCountdown.Widgets;

/// <summary>
/// The 2×2 countdown widget: its own entry in the widget picker. Everything else is inherited, so it is redrawn and
/// scheduled together with <see cref="CountdownWidgetProvider"/> (one alarm for both).
/// </summary>
[BroadcastReceiver(Name = "au.bigriversoftware.bathurstcountdown.SquareCountdownWidgetProvider", Label = "Bathurst Countdown (2×2)", Exported = true)]
[IntentFilter([AppWidgetManager.ActionAppwidgetUpdate])]
[MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/countdown_widget_square_info")]
public class SquareCountdownWidgetProvider : CountdownWidgetProvider
{
}
