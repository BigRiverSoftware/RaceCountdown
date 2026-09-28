using Microsoft.Extensions.Logging;
using RaceCountdown.Core.Feed;
using RaceCountdown.Services;
using RaceCountdown.ViewModels;
using RaceCountdown.Views;

namespace RaceCountdown;

public static class MauiProgram
{
    /// <summary>The feed snapshot bundled in Resources/Raw, used on a first launch with no network (plan D3).</summary>
    public const string BundledSnapshotName = "events.snapshot.json";

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                // Poppins (docs/design-system): Bold for the countdown and headings, Bold Italic for the wordmark.
                fonts.AddFont("Poppins-Regular.ttf", "PoppinsRegular");
                fonts.AddFont("Poppins-SemiBold.ttf", "PoppinsSemibold");
                fonts.AddFont("Poppins-Bold.ttf", "PoppinsBold");
                fonts.AddFont("Poppins-BoldItalic.ttf", "PoppinsBoldItalic");
            });

        var services = builder.Services;
        services.AddSingleton(TimeProvider.System);

        services.AddHttpClient(nameof(EventFeedClient), FeedHttp.Configure);
        services.AddSingleton<IEventFeedClient>(sp => new EventFeedClient(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(EventFeedClient)),
            EventFeedClient.DefaultFeedUrl));

        // The widgets (Phases 4 and 5) read the same cache folder.
        services.AddSingleton(_ => new FeedCache(FileSystem.Current.AppDataDirectory));
        services.AddSingleton(sp => new FeedStore(
            sp.GetRequiredService<FeedCache>(),
            sp.GetRequiredService<IEventFeedClient>(),
            _ => FileSystem.Current.OpenAppPackageFileAsync(BundledSnapshotName),
            sp.GetRequiredService<TimeProvider>()));

#if ANDROID
        services.AddSingleton<IWidgetUpdater, Widgets.AndroidWidgetUpdater>();
#elif WINDOWS
        services.AddSingleton<IWidgetUpdater, Widgets.WindowsWidgetUpdater>();
#else
        services.AddSingleton<IWidgetUpdater, NoWidgetUpdater>();
#endif
        services.AddSingleton(_ => Dispatcher.GetForCurrentThread()!);
        services.AddSingleton<CountdownViewModel>();
        services.AddTransient<CountdownPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
