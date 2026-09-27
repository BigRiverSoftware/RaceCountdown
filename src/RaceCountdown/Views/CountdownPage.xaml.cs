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
        var source = Root.Width > Root.Height ? "background_landscape.png" : "background_portrait.png";
        if ((BackgroundArt.Source as FileImageSource)?.File != source)
        {
            BackgroundArt.Source = source;
        }
    }

    protected override void OnDisappearing()
    {
        viewModel.Stop();
        base.OnDisappearing();
    }
}
