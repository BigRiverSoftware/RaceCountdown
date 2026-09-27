using System.Text;
using System.Text.Json;
using RaceCountdown.Core.Feed;
using RaceCountdown.Core.Models;

namespace RaceCountdown.Core.Tests;

public class FeedSerializerTests
{
    private const string PlanExample = """
        {
          "schemaVersion": 1,
          "generatedUtc": "2026-09-27T00:00:00Z",
          "series": [{ "id": "supercars", "name": "Repco Supercars Championship", "shortName": "Supercars", "accentColour": "#E10600" }],
          "tracks": [{ "id": "mount-panorama", "name": "Mount Panorama Circuit", "location": "Bathurst, NSW",
                       "countryCode": "AU", "timeZoneId": "Australia/Sydney", "latitude": -33.4475, "longitude": 149.5570 }],
          "events": [{
            "id": "supercars-2026-bathurst-1000", "seriesId": "supercars", "trackId": "mount-panorama",
            "name": "Bathurst 1000", "startDate": "2026-10-08", "endDate": "2026-10-11", "status": "Confirmed",
            "sessions": [{ "id": "race-30", "name": "Race 30", "type": "Race",
                           "startLocal": "2026-10-11T11:30:00", "startUtc": "2026-10-11T00:30:00Z",
                           "estimatedDuration": "06:30:00" }]
          }]
        }
        """;

    [Fact]
    public void Deserializes_the_plan_example()
    {
        var feed = FeedSerializer.Deserialize(PlanExample);

        var ev = Assert.Single(feed.Events);
        Assert.Equal(new DateOnly(2026, 10, 8), ev.StartDate);
        Assert.Equal(EventStatus.Confirmed, ev.Status);
        var session = Assert.Single(ev.Sessions);
        Assert.Equal(SessionType.Race, session.Type);
        Assert.Equal(TestFeeds.RaceStart2026, session.StartUtc);
        Assert.Equal(TimeSpan.FromHours(6.5), session.EstimatedDuration);
        Assert.Equal("Australia/Sydney", feed.FindTrack("mount-panorama")!.TimeZoneId);
        Assert.Equal("Supercars", feed.FindSeries("supercars")!.ShortName);
        Assert.Null(feed.FindTrack("nope"));
    }

    [Fact]
    public void Round_trips_and_writes_instants_as_utc_with_z()
    {
        var feed = TestFeeds.Feed(TestFeeds.Bathurst2026(), TestFeeds.Tba(2027));

        var json = FeedSerializer.Serialize(feed);
        var back = FeedSerializer.Deserialize(json);

        Assert.Contains("\"startUtc\": \"2026-10-11T00:30:00Z\"", json);
        Assert.Contains("\"status\": \"DateTba\"", json);
        Assert.DoesNotContain("endUtc", json);
        Assert.Contains("\"startDate\": null", json);
        Assert.Equal(json, FeedSerializer.Serialize(back));
    }

    [Fact]
    public void Reads_instants_with_any_offset_as_utc()
    {
        var json = PlanExample.Replace("2026-10-11T00:30:00Z", "2026-10-11T11:30:00+11:00");

        var session = FeedSerializer.Deserialize(json).Events[0].Sessions[0];

        Assert.Equal(TimeSpan.Zero, session.StartUtc.Offset);
        Assert.Equal(TestFeeds.RaceStart2026, session.StartUtc);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{ \"schemaVersion\": 1 }")]
    [InlineData("not json")]
    public void Rejects_malformed_feeds(string json)
    {
        Assert.ThrowsAny<JsonException>(() => FeedSerializer.Deserialize(json));
        Assert.False(FeedSerializer.TryDeserialize(json, out var feed));
        Assert.Null(feed);
    }

    [Fact]
    public void Rejects_a_null_required_value()
    {
        var json = PlanExample.Replace("\"name\": \"Race 30\"", "\"name\": null");

        Assert.ThrowsAny<JsonException>(() => FeedSerializer.Deserialize(json));
    }

    [Fact]
    public void TryDeserialize_accepts_a_good_feed()
    {
        Assert.True(FeedSerializer.TryDeserialize(PlanExample, out var feed));
        Assert.NotNull(feed);
    }

    [Fact]
    public async Task Deserializes_from_a_stream()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(PlanExample));

        var feed = await FeedSerializer.DeserializeAsync(stream);

        Assert.Single(feed.Events);
    }

    [Fact]
    public async Task Rejects_an_empty_stream_feed()
    {
        await using var stream = new MemoryStream("null"u8.ToArray());

        await Assert.ThrowsAsync<JsonException>(() => FeedSerializer.DeserializeAsync(stream));
    }
}
