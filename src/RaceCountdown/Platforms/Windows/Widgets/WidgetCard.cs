using System.Reflection;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Providers;
using RaceCountdown.Core.Widgets;
using Windows.ApplicationModel;

namespace RaceCountdown.Widgets;

/// <summary>
/// Builds the Widgets Board update for one widget: the Adaptive Card template for its size (embedded
/// <c>Templates/*.json</c>) and the data from <see cref="WidgetCardData"/>, with the widget art as a data URI.
/// </summary>
internal static class WidgetCard
{
    // The generated scale-200 PNG (640×320) is sharp on the board at 100–200% scaling and only about 34 KB.
    private const string BackgroundFile = "widget_background.scale-200.png";

    private static readonly Lazy<string?> Background = new(LoadBackground);

    private static readonly Dictionary<WidgetSize, Lazy<string>> Templates = new()
    {
        [WidgetSize.Small] = new(() => LoadTemplate("small")),
        [WidgetSize.Medium] = new(() => LoadTemplate("medium")),
        [WidgetSize.Large] = new(() => LoadTemplate("large")),
    };

    public static WidgetUpdateRequestOptions Build(string widgetId, WidgetSize size, WidgetSnapshot snapshot) =>
        new(widgetId)
        {
            Template = (Templates.GetValueOrDefault(size) ?? Templates[WidgetSize.Medium]).Value,
            Data = WidgetCardData.ToJson(snapshot, Background.Value),
        };

    private static string LoadTemplate(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"RaceCountdown.Widgets.Templates.{name}.json")
            ?? throw new InvalidOperationException($"Missing widget template '{name}'.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string? LoadBackground()
    {
        try
        {
            var path = Path.Combine(Package.Current.InstalledLocation.Path, BackgroundFile);
            return "data:image/png;base64," + Convert.ToBase64String(File.ReadAllBytes(path));
        }
        catch (IOException)
        {
            // The card falls back to the board's own background.
            return null;
        }
    }
}
