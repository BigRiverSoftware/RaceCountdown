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

## Phase 2 — App: complete (2026-09-27)

| Item | Status |
|------|--------|
| Core: `EventFeedClient` (ETag / `If-None-Match`, 20 s timeout, re-validates on device) | Done |
| Core: `FeedCache` (atomic files in app data, shared with the widgets later) and `FeedStore` (cache or bundled snapshot, whichever is newer, then background refresh) | Done |
| Core: `RefreshPolicy` (start-up, 12-hourly, hourly in race week, after the race starts and finishes, 15 min retry) | Done |
| Core: `CountdownDisplay` — all page text for every state, unit-tested | Done |
| App: `CountdownViewModel` (1 s tick while visible, stops in the background), `CountdownPage` (segmented countdown, RACE DAY / RACE UNDERWAY / TBA, start time in track and local time, footer, pull-to-refresh + Refresh button, wide layout ≥ 720 px) | Done |
| Bundled `Resources/Raw/events.snapshot.json` (the 2026 race) | Done |
| DI in `MauiProgram`; placeholder `MainPage` removed; `IWidgetUpdater` no-op until Phases 4–5 | Done |
| `tools/dev/Register-WindowsApp.ps1` — deploys the packaged Windows app from the command line | Done |

**Verification:**
- `dotnet build RaceCountdown.slnx`: 0 warnings, 0 errors. Core tests: 120 passed, **99.5% line / 96.6% branch coverage**.
  Builder tests unchanged (35 passed).
- **Windows 11** (packaged, via the dev script): cold start with no feed online showed 13 d 23 h 09 m to 11 Oct
  11:30 AEDT from the bundled snapshot, ticking each second.
- **Android API 26 emulator** (Release APK): same result, 13 d 22 h 30 m at 11:59 AEST.
- **Roll-over** after a simulated race start is covered by `FeedStoreTests.Rolls_over_to_next_years_race_after_the_race_finishes`
  (fake clock: Counting 2026 → Live → Counting 2027) rather than on a device; the app has no hidden clock override.
- Not yet seen: a real download, because the feed is not published until the repo is pushed and Pages is enabled.
  Until then every start-up refresh fails and the app correctly keeps the bundled data.

**Notes:**
- A Debug APK installed with `adb install` crashes at start ("No assemblies found … Fast Deployment"). That is normal for
  Debug builds; use `dotnet build -t:Run` or a Release APK.
- The API 34 Play Store emulator image hung on every `adb install`; the API 26 image worked. Retry API 34 in Phase 4.
- Decisions taken without asking are listed in [DECISIONS.md](DECISIONS.md).

## Phase 3 — Branding: complete (2026-09-27)

| Item | Status |
|------|--------|
| App icon: sunset + ridgeline background, circuit ribbon + checkered flag foreground (adaptive, inside the 66% safe circle) | Done |
| Splash: circuit outline on deep purple | Done |
| Page background: portrait and landscape crops (sky, sun, ridges, lit circuit on Mount Panorama, speed streaks, gum trees), chosen by window shape | Done |
| Widget background art for Phases 4–5 | Done |
| Readability overlay + content card; [`tools/dev/check_contrast.py`](../tools/dev/check_contrast.py) passes WCAG AA for every text style | Done |
| Android status bar and template colours on the palette | Done |

**Verification:**
- `dotnet build RaceCountdown.slnx`: 0 warnings, 0 errors; all tests pass.
- `check_contrast.py`: all 8 text styles pass (lowest: muted 12 px footer 4.75:1; gold RACE DAY banner 3.34:1 as large text).
- Icon checked at 300/96/48 px, square and round, against the 66% safe circle, and in the API 26 app drawer.
- Countdown page checked on Windows 11 (landscape, 960×640) and the API 26 emulator (portrait, Release APK).
  The first phone layout clipped the seconds segment; fixed.
- Not checked on every density on a real device; resizetizer generates all Android densities and Windows scales
  from the same SVGs.

**Note:** resizetizer kept a stale icon PNG after the SVG changed. Delete `obj/.../resizetizer` after editing art
(see [assets-src/README.md](../assets-src/README.md)).
