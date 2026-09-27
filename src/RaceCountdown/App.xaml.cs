using RaceCountdown.ViewModels;

namespace RaceCountdown;

public partial class App : Application
{
    private readonly CountdownViewModel countdown;

    public App(CountdownViewModel countdown)
    {
        InitializeComponent();
        this.countdown = countdown;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell()) { Title = "Bathurst Countdown" };

#if WINDOWS
        window.Width = 960;
        window.Height = 640;
        window.MinimumWidth = 360;
        window.MinimumHeight = 480;
#endif

        // Stop the one-second tick in the background; on return, catch up and refresh if due (plan §7).
        window.Stopped += (_, _) => countdown.Stop();
        window.Resumed += async (_, _) => await countdown.StartAsync();

        return window;
    }
}
