using System.Text.Json;
using System.Text.RegularExpressions;

namespace EventFeedBuilder;

/// <summary>
/// Reads the data that Next.js App Router pages embed as <c>self.__next_f.push([1,"..."])</c> scripts
/// (spike S1). Returns every JSON object found anywhere in that data.
/// </summary>
public static partial class NextFlightData
{
    [GeneratedRegex("""self\.__next_f\.push\(\[1,("(?:[^"\\]|\\.)*")\]\)""")]
    private static partial Regex PushPattern();

    public static IEnumerable<JsonElement> ExtractObjects(string html)
    {
        var payload = string.Concat(
            PushPattern().Matches(html).Select(m => JsonSerializer.Deserialize<string>(m.Groups[1].Value)));

        foreach (var line in payload.Split('\n'))
        {
            var colon = line.IndexOf(':');
            if (colon < 0 || colon + 1 >= line.Length || line[colon + 1] is not ('[' or '{'))
            {
                continue;
            }

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(line.AsMemory(colon + 1));
            }
            catch (JsonException)
            {
                // Some lines are React component references rather than data; they are not needed.
                continue;
            }

            foreach (var obj in Walk(document.RootElement))
            {
                yield return obj;
            }
        }
    }

    private static IEnumerable<JsonElement> Walk(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                yield return element;
                foreach (var property in element.EnumerateObject())
                {
                    foreach (var child in Walk(property.Value))
                    {
                        yield return child;
                    }
                }

                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    foreach (var child in Walk(item))
                    {
                        yield return child;
                    }
                }

                break;
        }
    }
}
