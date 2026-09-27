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

    protected override void OnDisappearing()
    {
        viewModel.Stop();
        base.OnDisappearing();
    }
}
