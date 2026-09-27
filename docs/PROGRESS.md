# Progress

## Phase 0 — Groundwork: complete (2026-09-27)

| Item | Status |
|------|--------|
| App moved to `src/RaceCountdown/`; iOS and MacCatalyst removed | Done |
| Target frameworks `net10.0-android` + `net10.0-windows10.0.19041.0`; Android minimum API 26 | Done |
| Identity: `au.bigriversoftware.bathurstcountdown`, MSIX `BigRiverSoftware.BathurstCountdown` / `CN=Big River Software`, title "Bathurst Countdown" | Done |
| `WindowsPackageType=MSIX`, `MsixPackage` launch profile, `internetClient` capability | Done |
| Projects: `src/RaceCountdown.Core`, `tests/RaceCountdown.Core.Tests`, `tools/EventFeedBuilder`, `tests/EventFeedBuilder.Tests` (all empty, ready for Phase 1) | Done |
| Placeholder `MainPage` (template bot image removed) | Done |
| CI: [`.github/workflows/build.yml`](../.github/workflows/build.yml) builds both targets and runs the tests on `windows-latest` | Written. **It has not run on GitHub yet**, because the repo has no GitHub remote. |
| Spike S1: supercars.com data | [Concluded](spikes/S1-supercars-data.md) |
| Spike S2: Windows widget host | [Concluded, with one item left for hands-on testing in Phase 5](spikes/S2-windows-widget-host.md) |

**Verification:** a local `dotnet build RaceCountdown.slnx` (Android + Windows) succeeds with 0 warnings and 0 errors, and `dotnet test` runs. The CI steps are the same commands, but the workflow itself hasn't run until the repo is pushed.

**Housekeeping:** the old `RaceCountdown/` folder holds only `bin/`, `obj/` and `.csproj.user`, which Visual Studio had locked. It's ignored by git. Delete it once VS is closed.

## Phase 1 — Core and feed: code complete (2026-09-27), feed not yet live

| Item | Status |
|------|--------|
| Core models (`Series`, `Track`, `Session`, `RaceEvent`, `EventFeed`) and source-generated `FeedSerializer` | Done |
| `FeedValidator` (ids, references, time zones, `startUtc` vs `startLocal`, sessions inside event dates) | Done |
| `EventFilter`, `NextSessionSelector`, `CountdownCalculator` (`Counting` / `RaceDay` / `Live` / `AwaitingSchedule` + stale flag) | Done |
| `StartTimeFormatter` ("Sun 11 Oct, 11:30 AEDT · 08:30 AWST (your time)") and `WidgetSnapshot` | Done |
| Core tests: 69 tests, **100% line / 97.7% branch coverage**, including the NSW DST change on 4 Oct 2026 | Done |
| `feed/events.schema.json`, `feed/overrides.json`, [`feed/README.md`](../feed/README.md) | Done |
| `EventFeedBuilder`: fetch → parse → merge overrides → schema + validator + 7-day-move checks → `events.json` | Done |
| Builder tests: 35 tests against pages saved from supercars.com on 2026-09-27 | Done |
| [`publish-feed.yml`](../.github/workflows/publish-feed.yml): daily, plus 6-hourly in race week, deploys to GitHub Pages | Written, not yet run |
| Feed live with the 2026 race at 2026-10-11 11:30 Australia/Sydney | **Waiting on push + Pages** (see below) |

**Verification:** `dotnet build RaceCountdown.slnx` has 0 warnings and 0 errors, and all 104 tests pass. A live
run of the builder against supercars.com produced `Race 30` at `2026-10-11T11:30:00` track time (`00:30Z`) and passed
every check.

**Design notes:**
- "Stale" is an `IsStale` flag, not a phase, because the countdown is still shown (plan §6).
- The feed writes nulls explicitly (`"startDate": null`), and every field is required, so a truncated or
  malformed feed is rejected on the device instead of being read with missing values.
- `overrides.json` also holds the series and track reference data, because the scraped pages have no time zones
  or coordinates.
- Between seasons, if the calendar has no upcoming Bathurst 1000, the builder publishes an undated `DateTba`
  placeholder, so the app shows "TBA" (D13).
- `build.yml` now triggers on `dev`, the repo's default branch. It previously listed `master`/`main`.

**To go live:** push `dev`, then enable GitHub Pages with source **GitHub Actions**. The push triggers
*Publish feed*, which deploys `events.json`.
