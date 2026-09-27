using Microsoft.UI.Dispatching;
using RaceCountdown.Widgets;

namespace RaceCountdown.WinUI;

/// <summary>
/// Replaces the XAML-generated entry point (<c>DISABLE_XAML_GENERATED_MAIN</c> in the project) so the same exe can
/// start either the app or, when the Widgets Board asks, only the widget provider (plan D5).
/// </summary>
public static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        if (WidgetComServer.IsRequested(args))
        {
            WidgetComServer.Run();
            return;
        }

        Microsoft.UI.Xaml.Application.Start(_ =>
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
            new App();
        });
    }
}
