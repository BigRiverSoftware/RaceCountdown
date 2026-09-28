# Event feed

The app reads `https://bigriversoftware.github.io/RaceCountdown/events.json`. The
[Publish feed](../.github/workflows/publish-feed.yml) workflow builds it from supercars.com and the files in this
folder, checks it, and deploys it to GitHub Pages.

| File | Purpose |
|------|---------|
| `events.schema.json` | JSON Schema for `events.json`. Also published next to the feed. |
| `overrides.json` | Hand-curated data: series, tracks, and event corrections. Comments are allowed. |

## When the workflow runs
- Daily at 18:17 UTC.
- At 00:17, 06:17 and 12:17 UTC as well, but these runs only publish within 7 days of the race.
- On every push to `dev` that changes the feed, the builder or Core.
- By hand: **Actions → Publish feed → Run workflow**.

If a check fails, the run fails, GitHub emails you, and the previous feed stays live.

## Correcting a start time
Add the whole event to `events` in `overrides.json`. It replaces the scraped event with the same `id`. An
overridden event is also exempt from the "start time moved by more than 7 days" check.

```jsonc
"events": [
  {
    "id": "supercars-2026-bathurst-1000",
    "seriesId": "supercars",
    "trackId": "mount-panorama",
    "name": "Bathurst 1000",
    "startDate": "2026-10-08",
    "endDate": "2026-10-11",
    "status": "Confirmed",
    "sessions": [
      {
        "id": "race-30",
        "name": "Race 30",
        "type": "Race",
        "startUtc": "2026-10-11T00:30:00Z",   // Must match startLocal in the track's time zone.
        "startLocal": "2026-10-11T11:30:00",
        "estimatedDuration": "07:00:00"
      }
    ]
  }
]
```

Remove the override once supercars.com shows the right time again.

## Running the builder locally

```sh
# Live (fetches two or three pages from supercars.com):
dotnet run --project tools/EventFeedBuilder -- --out site

# Offline, from the saved test pages:
dotnet run --project tools/EventFeedBuilder -- --out site --source-dir tests/EventFeedBuilder.Tests/Fixtures
```

## When supercars.com changes
The parser tests run against pages saved in `tests/EventFeedBuilder.Tests/Fixtures`. If the site changes and the
workflow starts failing, save fresh copies of `/calendar` and `/events/<year>-bathurst-1000`, fix the parser, and
update the tests.
