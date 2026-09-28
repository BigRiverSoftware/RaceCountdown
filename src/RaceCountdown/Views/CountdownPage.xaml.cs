using RaceCountdown.ViewModels;

namespace RaceCountdown.Views;

public partial class CountdownPage : ContentPage
{
    private readonly CountdownViewModel viewModel;

    public CountdownPage(CountdownViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.StartAsync();
    }

    private void OnRootSizeChanged(object? sender, EventArgs e)
    {
        var landscape = Root.Width > Root.Height;
        var source = landscape ? "background_landscape.png" : "background_portrait.png";
        if ((BackgroundArt.Source as FileImageSource)?.File != source)
        {
            BackgroundArt.Source = source;
        }

        // Crop the art from the top, never the bottom, so the road and wall stay in view (docs/design-system).
        // AspectFill alone trims top and bottom equally when the window is wider than the art, so let the image
        // run up past the top of the window instead.
        var artAspect = landscape ? 1920.0 / 1080 : 1080.0 / 1920;
        var fullHeight = Root.Width / artAspect;
        BackgroundArt.Margin = new Thickness(0, Math.Min(0, Root.Height - fullHeight), 0, 0);
    }

    protected override void OnDisappearing()
    {
        viewModel.Stop();
        base.OnDisappearing();
    }
}
