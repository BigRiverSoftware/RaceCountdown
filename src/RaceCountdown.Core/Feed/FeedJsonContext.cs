using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using RaceCountdown.Core.Models;

namespace RaceCountdown.Core.Feed;

// Source-generated so the feed can be (de)serialised on trimmed Android builds without reflection.
// Every constructor parameter is required and nulls are written explicitly ("startDate": null), so a feed
// with a missing field is rejected instead of silently read as null.
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    WriteIndented = true,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true,
    Converters = [typeof(UtcDateTimeOffsetConverter)])]
[JsonSerializable(typeof(EventFeed))]
[ExcludeFromCodeCoverage]
internal sealed partial class FeedJsonContext : JsonSerializerContext;
