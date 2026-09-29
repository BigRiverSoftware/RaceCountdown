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
| [`publish-feed.yml`](../.github/workflows/publish-feed.yml): daily or run by hand, deploys to GitHub Pages | Written, not yet run |
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

## Phase 4 — Android widget: complete (2026-09-27)

| Item | Status |
|------|--------|
| `CountdownWidgetProvider` (receiver `au.bigriversoftware.bathurstcountdown.CountdownWidgetProvider`), medium and compact layouts, responsive on Android 12+ | Done |
| Race-day hand-over to a system-ticked `Chronometer` (final 24 h), "RACE UNDERWAY" while live, "TBA" after | Done |
| Redraw scheduling in Core (`WidgetSchedule`, tested) + one inexact alarm | Done |
| `FeedRefreshWorker` (WorkManager, 12 h + on demand via `RefreshPolicy`) | Done |
| `WidgetSystemReceiver`: boot, time set, time-zone change, app update; `RECEIVE_BOOT_COMPLETED` permission | Done |
| App redraws widgets after it downloads a new feed (`AndroidWidgetUpdater`); tapping the widget opens the app | Done |

**Verification** (Release APK):
- Build: 0 warnings, 0 errors. Core tests: 133 passed (`WidgetScheduleTests` added).
- **API 26 emulator** checklist:
  - Added from the picker: "13 days / 21 hours", with the alarm set for 13:30:01 (when the hours shown change).
  - Clock set to 10 Oct 20:00 AEDT: switched at once to a ticking chronometer, 15:29:53, "RACE DAY".
  - Clock set to 30 min after the start: "RACE UNDERWAY" (the headline first wrapped to "RACE"; fixed with auto-sizing text).
  - Clock set after the estimated finish: rolled over to "TBA", with the "Offline" line because the feed isn't published yet and the bundled data is over 14 days old.
  - Reboot: widget redrawn with the real countdown and the alarm re-armed. App update: widget redrawn at once.
  - WorkManager job registered.
- **API 34 emulator** (a cold boot fixed the earlier install hang): added from the Pixel launcher picker, rounded
  corners, and resizing to 2 columns switched to the compact "13 / DAYS TO GO" layout.

**Not verified:**
- API 35: no API 35 system image is installed; API 34 was used instead.
- Airplane mode as a separate test. The feed isn't online yet, so every refresh during these tests already failed
  and the widget kept working from the cache and bundled data.
- A real feed download by the worker. That waits on the feed going live.

## Phase 5 — Windows widget: complete (2026-09-27)

| Item | Status |
|------|--------|
| Custom `Program.Main` (`DISABLE_XAML_GENERATED_MAIN`): starts the MAUI app, or with `-RegisterProcessAsComServer` only the widget provider | Done |
| `WidgetComServer`: `CoRegisterClassObject` for CLSID `AF609A3B-…`, source-generated `IClassFactory`, exits when the last widget is removed | Done |
| `CountdownWidgetProvider : IWidgetProvider` (create, delete, resize, activate/deactivate, click opens the app) | Done |
| Adaptive Card templates, small / medium / large, on the widget art | Done |
| Core: `WidgetSchedule.NextMinuteRedrawUtc` and `WidgetCardData` (tested) | Done |
| Manifest: `com:ExeServer` + `windows.appExtension` widget definition `BathurstCountdown.Countdown` | Done |
| `WindowsWidgetUpdater`: the app redraws pinned widgets after it downloads a feed | Done |
| Pinned from the Widgets Board, updates, survives a reboot | Done (checked by hand on Windows 11) |

**Verification:**
- `dotnet build RaceCountdown.slnx`: 0 warnings, 0 errors. Core tests: 143 passed (10 new); builder tests: 35 passed.
- Packaged app registered with `tools/dev/Register-WindowsApp.ps1`; the generated `AppxManifest.xml` has the COM server and widget definition.
- COM activation, without the board: creating the CLSID starts `RaceCountdown.exe -RegisterProcessAsComServer -Embedding`
  with no window and returns the provider. (The first try found `GetWidgetInfos()` returns null when no widget is pinned; fixed.)
- A normal launch still opens the app window through the custom `Main`, so spike S2's duplicate-`Main` concern is closed.
- **Windows 11, by hand:** pinned from the Widgets Board picker; small, medium and large layouts shown correctly and
  readable on the artwork; the minutes ticked down while the board was open; clicking opened the app; the widget was
  still pinned and up to date after a reboot.

**Note:** `tools/dev/Register-WindowsApp.ps1` uninstalls the package before registering, which also removes pinned widgets.
