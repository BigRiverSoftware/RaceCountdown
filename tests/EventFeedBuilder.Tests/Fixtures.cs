namespace EventFeedBuilder.Tests;

internal static class Fixtures
{
    public static string Directory => Path.Combine(AppContext.BaseDirectory, "Fixtures");

    public static string Read(string name) => File.ReadAllText(Path.Combine(Directory, name));

    public static string FeedFile(string name) => Path.Combine(AppContext.BaseDirectory, "feed", name);

    public static FeedOverrides Overrides() => FeedOverrides.Load(FeedFile("overrides.json"));

    /// <summary>The fixtures were saved on this date.</summary>
    public static readonly DateTimeOffset SavedOn = new(2026, 9, 27, 0, 0, 0, TimeSpan.Zero);
}
