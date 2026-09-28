# Race Countdown — Implementation Plan

## 1. Summary

A single-purpose .NET MAUI app for **Windows and Android** that counts down to the start of the next Supercars race at **Mount Panorama, Bathurst**. It gets race times from public web sources, moves on to the next race by itself once one has started, and has a native home-screen widget on both platforms (Android App Widget, Windows 11 Widgets Board).

Version 1 shows one event. The data model, data feed and UI are built so that more events, series and tracks around the world can be added later without a rewrite.

### Goals
- Accurate countdown to the green-flag time of the next Bathurst Supercars race, correct in every time zone and across daylight-saving changes.
- No manual updates: the app finds the next race start time on its own.
- Native widgets on Android and Windows 11.
- Bold, colourful branding that says both *motor racing* and *Mount Panorama*.
- Works offline once it has cached data.

### Non-goals (v1)
- iOS and macOS. The Apple target frameworks and platform folders will be removed.
- Live timing, results, news or streaming.
- User accounts and push notifications. A "race starting soon" notification is a possible later addition.
- Using official Supercars or Bathurst 1000 logos or trademarks.

---

## 2. Key decisions

| # | Decision | Choice | Rationale |
|---|----------|--------|-----------|
| D1 | Target platforms | `net10.0-android`, `net10.0-windows10.0.19041.0` only | Requirement; removing iOS and Mac simplifies the build and CI. |
| D2 | Where race data comes from | **A hosted JSON event feed** that a scheduled GitHub Action builds from public sources. The app reads the feed and does not scrape websites itself. | supercars.com is a JavaScript-rendered Next.js site with no public API or ICS feed. Scraping it from every installed app is brittle and a poor way to treat the site. Scraping once a day on a server, then validating and publishing the result, means a site change breaks the pipeline, not every installed app. |
| D3 | Fallback data | Last good feed cached on the device, plus a snapshot bundled with the app | The countdown still works offline and on first launch with no network. |
| D4 | Windows packaging | **MSIX (packaged)**. `WindowsPackageType` changes from `None` to `MSIX`. | Windows 11 widget providers must be packaged apps. |
| D5 | Windows widget host | A COM widget provider (`IWidgetProvider`) **inside the app's own exe**, activated with `-RegisterProcessAsComServer`. Fallback: a separate small provider exe in the same package. | One exe keeps packaging simple. The fallback is only needed if MAUI/WinUI startup gets in the way of COM activation (see spike S2). |
| D6 | Android widget ticking | `RemoteViews` `Chronometer` counting down (API 24+) for the final 24 hours. Before that, the widget shows days and hours as text and refreshes on schedule. | The widget ticks live with no app wake-ups and little battery use. |
| D7 | Android minimum SDK | Raise from 21 to **26** (Android 8.0) | Chronometer countdown, adaptive icons and modern background-work limits. Almost no devices in use are below this. |
| D8 | Architecture | MVVM with `CommunityToolkit.Mvvm`. A platform-neutral `RaceCountdown.Core` class library for models, the feed client and countdown logic. | Core logic can be unit-tested without a device, and the app and both widgets share it. |
| D9 | Time handling | Store the UTC instant, the local wall-clock time and the track's IANA time zone. Use `TimeProvider` for testability. | Countdown maths runs on UTC instants. The track-local time is kept for display and for checking the data. |
| D10 | Publisher and app identity | Publisher **Big River Software**. App id **`au.bigriversoftware.bathurstcountdown`**. Windows identity details are in §10. | Reverse-DNS form of the company domain `bigriversoftware.au`, unique to this app. Kept stable forever, because changing it makes the app a different app. |
| D11 | Distribution (v1) | **Sideloaded only**: a signed APK for Android and a signed MSIX for Windows, both attached to GitHub Releases. | Your choice. Store publishing can come later without changing the app id. |
| D12 | Which race counts | **Only the Bathurst 1000 main race.** Filter: series `supercars`, track `mount-panorama`, event name contains "Bathurst 1000", session type `Race`. | Your choice. Practice, qualifying, the Top 10 Shootout and support categories are ignored. |
| D13 | Between seasons | Show **"TBA"** until next year's race start time is published. No estimated dates. | Your choice. The app never shows a countdown to a guessed date. |
| D14 | Feed hosting | **GitHub Pages** on this repository, so the repository will be public. | Your choice. Free, versioned, and published straight from the feed Action. |

---

## 3. Solution structure

```
RaceCountdown.slnx
├─ src/
│  ├─ RaceCountdown/                  # MAUI app (Android + Windows)
│  │  ├─ Views/                       # CountdownPage (later: EventListPage, SettingsPage)
│  │  ├─ ViewModels/                  # CountdownViewModel
│  │  ├─ Services/                    # Platform glue: IWidgetUpdater, background refresh scheduling
│  │  ├─ Platforms/Android/
│  │  │  ├─ Widgets/                  # CountdownWidgetProvider (AppWidgetProvider), layouts
│  │  │  ├─ Work/                     # FeedRefreshWorker (WorkManager)
│  │  │  └─ Resources/xml/            # appwidget-provider metadata
│  │  ├─ Platforms/Windows/
│  │  │  ├─ Widgets/                  # CountdownWidgetProvider (IWidgetProvider), COM factory
│  │  │  ├─ Widgets/Templates/        # Adaptive Card JSON templates
│  │  │  └─ Package.appxmanifest      # COM server + widget AppExtension declarations
│  │  └─ Resources/                   # AppIcon, Splash, Images, Raw/events.snapshot.json
│  │
│  └─ RaceCountdown.Core/             # net10.0 class library, no MAUI dependency
│     ├─ Models/                      # Series, Track, RaceEvent, Session, EventFeed
│     ├─ Feed/                        # IEventFeedClient, FeedValidator, FeedCache
│     ├─ Countdown/                   # NextSessionSelector, CountdownCalculator, CountdownState
│     └─ Widgets/                     # WidgetSnapshot (platform-neutral widget view model)
│
├─ tests/
│  └─ RaceCountdown.Core.Tests/       # xUnit tests for everything in Core
│
├─ tools/
│  └─ EventFeedBuilder/               # Console app run by CI: fetches sources → validates → writes events.json
│
├─ feed/
│  ├─ events.schema.json              # JSON Schema for the feed
│  └─ overrides.json                  # Hand-curated corrections, always win over scraped data
│
├─ assets-src/                        # Source artwork (SVG) for icon, splash, backgrounds, widget art
├─ docs/PLAN.md
└─ .github/workflows/
   ├─ build.yml                       # Build + test app on every push
   ├─ publish-feed.yml                # Daily schedule: build feed, validate, publish to GitHub Pages
   └─ release.yml                     # On version tag: build + sign APK and MSIX, attach to GitHub Release
```

> The existing template project sits at `RaceCountdown/`. It moves to `src/RaceCountdown/` in Phase 0.

---

## 4. Data model (future-proofed for many events)

```csharp
record Series(string Id, string Name, string? ShortName, string AccentColour);      // "supercars"
record Track(string Id, string Name, string Location, string CountryCode,
             string TimeZoneId, double Latitude, double Longitude);                // "mount-panorama", "Australia/Sydney"
record Session(string Id, string Name, SessionType Type,
               DateTimeOffset StartUtc, string StartLocal, TimeSpan? EstimatedDuration);
record RaceEvent(string Id, string SeriesId, string TrackId, string Name,
                 DateOnly StartDate, DateOnly EndDate, EventStatus Status,
                 IReadOnlyList<Session> Sessions);
record EventFeed(int SchemaVersion, DateTimeOffset GeneratedUtc,
                 IReadOnlyList<Series> Series, IReadOnlyList<Track> Tracks,
                 IReadOnlyList<RaceEvent> Events);

enum SessionType { Practice, Qualifying, Shootout, Race }
enum EventStatus { Confirmed, Provisional, DateTba, Cancelled }
```

Example feed record:

```json
{
  "schemaVersion": 1,
  "generatedUtc": "2026-09-27T00:00:00Z",
  "series": [{ "id": "supercars", "name": "Supercars Championship", "shortName": "Supercars", "accentColour": "#E10600" }],
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
```

**Future multi-event support is built in from the start:**
- The app keeps a list of "followed" filters (by series, track or event). v1 hard-codes one (D12): `series = supercars AND track = mount-panorama AND event name contains "Bathurst 1000" AND sessionType = Race`.
- The UI is driven by `IReadOnlyList<CountdownItem>`. v1 shows the first item. Later versions show a carousel or list and a picker.
- Widgets store their filter per widget instance, so different widgets can show different events later.
- Adding a new track or series means adding data to the feed. No app release is needed.

---

## 5. Data acquisition

### 5.1 Feed pipeline (server side, `tools/EventFeedBuilder` + GitHub Actions)
1. **Fetch** from the sources listed below, in priority order.
2. **Parse** each source into the `RaceEvent`/`Session` model.
3. **Merge**. Precedence: `overrides.json` > official Supercars event page > secondary sources.
4. **Validate**:
   - JSON Schema check.
   - `startUtc` must match `startLocal` converted with the track's `TimeZoneId`.
   - The race start must fall inside the event's `startDate`..`endDate`.
   - A start time must not move by more than 7 days since the last publish without a manual override. This protects against a bad parse.
   - At least one future Bathurst 1000 main race must exist, or the event must be marked `DateTba`. `DateTba` is the normal state between seasons; the app shows "TBA" and no countdown (D13).
5. **Publish** `events.json` (plus an ETag/hash) to GitHub Pages. If validation fails, publish nothing: the job fails, GitHub notifies the maintainer, and the previous good feed stays live.
6. **Schedule**: daily all year, and every 6 hours during race week.

### 5.2 Candidate public sources
| Source | What it gives | Notes |
|--------|---------------|-------|
| `supercars.com/calendar` | Event names, dates, event URLs | JavaScript-rendered (Next.js). Parse the rendered HTML or the embedded/API data the page uses, and check the network calls during spike S1. |
| `supercars.com/events/<year>-bathurst-1000` | Session schedule, e.g. "Sun 11 Oct 11:30 AM" (track-local, no zone given) | This is where the race start time comes from. Read it as `Australia/Sydney` local time. |
| Wikipedia "<year> Supercars Championship" (MediaWiki API) | Calendar dates | Stable, structured fallback for dates. Usually has no start times. |
| `overrides.json` (hand-curated) | Anything | Lets the maintainer fix data immediately without an app release. |

The pipeline checks `robots.txt` and the site's terms, sends an honest User-Agent, and fetches only a few pages a day.

### 5.3 Client side (app)
- `IEventFeedClient` does an HTTP GET of the feed URL with `If-None-Match` and a timeout.
- `FeedCache` stores the last validated feed in `FileSystem.AppDataDirectory`. It is shared with the widgets. On Windows the widget provider runs in the same package, so it can use `ApplicationData.LocalFolder`.
- Load order: **cache → bundled snapshot**. A network refresh then runs in the background and updates the UI when it finishes.
- **Refresh policy:**
  - When the app starts.
  - Every 12 hours in the background (Android WorkManager periodic job; on Windows, when the widget provider is activated or the app starts).
  - Every hour during race week.
  - Once, immediately after the selected race starts, to pick up the next event.
- The app validates the feed again on the device (schema version, sanity checks) and ignores anything invalid.

---

## 6. Countdown logic (`RaceCountdown.Core`)

**`NextSessionSelector`** returns the next matching session where `StartUtc + EstimatedDuration > now`.

`CountdownState` drives the UI and the widgets:

| State | Condition | Display |
|-------|-----------|---------|
| `Counting` | now < start − 24h | `DD days HH:MM:SS` |
| `RaceDay` | start − 24h ≤ now < start | Large `HH:MM:SS`, "RACE DAY" accent |
| `Live` | start ≤ now < start + estimated duration | "RACE UNDERWAY" with elapsed time |
| `AwaitingSchedule` | No future Bathurst 1000 main race with a confirmed start time | "Next Bathurst 1000: **TBA**". No countdown, no estimated date (D13). If only the event dates are known, it shows those dates under "TBA", still with no countdown. |
| `Stale` | Feed older than 14 days and the network is failing | Countdown still shown, with a small "offline — last updated …" note |

- Uses `TimeProvider` everywhere so tests can fix the time.
- Shows the start time in **both** the user's local time and Bathurst local time, e.g. "Sun 11 Oct, 11:30 AEDT · 8:30 AWST (your time)".
- Tests must cover the NSW DST transitions. DST starts on the first Sunday in October, which often falls in or near Bathurst week.

---

## 7. App UI

- **One page** (`CountdownPage`): full-bleed Mount Panorama background, series and event name, big segmented countdown (days / hours / minutes / seconds), start time in local and track time, a small "last updated" footer.
- A `DispatcherTimer` ticks once per second while the page is visible and stops when the app is backgrounded.
- Supports light and dark themes and resizes from phone portrait to a large Windows window (uses `VisualStateManager` adaptive states).
- A pull-to-refresh or refresh button triggers a manual feed refresh.
- Accessibility: `SemanticProperties` on the countdown, e.g. "13 days, 4 hours until the Bathurst 1000". Numbers use a tabular (fixed-width) font so they don't jitter.
- Future: `EventListPage` and `SettingsPage` for followed events and preferences. v1 has no routes for them, but the Shell is set up so they can be added.

---

## 8. Widgets

### 8.1 Shared
`WidgetSnapshot` (in Core) turns the next session plus `now` into widget-ready text: title, days, time string, target instant, state. Both widget implementations render this snapshot, so neither contains countdown logic of its own.

### 8.2 Android — App Widget
- `CountdownWidgetProvider : AppWidgetProvider` with `[BroadcastReceiver]` and `[IntentFilter(APPWIDGET_UPDATE)]`, and `Resources/xml/countdown_widget_info.xml` for its metadata.
- **Sizes**:
  - Small, 2×1: days remaining.
  - Medium, 4×2: title, days, hh:mm, and a background image.
  - Resizable, with responsive `RemoteViews` layouts on API 31+.
- **Ticking**:
  - More than 24 hours out: text shows days and hours. An inexact `AlarmManager` alarm fires at the next hour boundary to refresh the text. No exact-alarm permission is needed.
  - Final 24 hours: `RemoteViews.SetChronometer(base, null, true)` + `SetChronometerCountDown(true)`. The system ticks it with no wake-ups.
  - Once the race is live, the widget switches to "RACE UNDERWAY".
- The **data refresh** is done by `FeedRefreshWorker` (WorkManager, 12h, network constraint). When the feed changes, the worker calls `AppWidgetManager.UpdateAppWidget`.
- Tapping the widget opens the app with a `PendingIntent`.
- **Permissions**: `INTERNET`, and `RECEIVE_BOOT_COMPLETED` so schedules are restored after a reboot.

### 8.3 Windows — Widgets Board (Windows 11)
- NuGet package: `Microsoft.WindowsAppSDK`, which is already part of the MAUI Windows target. The provider API is in `Microsoft.Windows.Widgets.Providers`.
- `CountdownWidgetProvider : IWidgetProvider` implements `CreateWidget`, `DeleteWidget`, `OnActionInvoked`, `OnWidgetContextChanged`, `Activate` and `Deactivate`. `IWidgetProvider2` is optional and adds customisation later.
- The card UI is **Adaptive Card** JSON templates (small / medium / large) plus a data JSON built from `WidgetSnapshot`.
- **Updates**: the Widgets Board is not meant to redraw every second. While a widget is active, a timer calls `WidgetManager.UpdateWidget` every minute. It shows minutes-level detail when more than an hour out, and updates more often in the final hour.
- **Manifest** (`Package.appxmanifest`):
  - `com:Extension` / `com:ExeServer` registers the provider's COM class ID with `Arguments="-RegisterProcessAsComServer"`.
  - `uap3:Extension Category="windows.appExtension"` with `Name="com.microsoft.windows.widgets"` declares the widget definition: id, display name, description, sizes, icons and screenshots.
- **Startup**: a custom `Main` (`DISABLE_XAML_GENERATED_MAIN`) checks for `-RegisterProcessAsComServer`.
  - If present, it registers the class factory with `CoRegisterClassObject` and runs the provider message loop without starting the MAUI UI.
  - Otherwise, it starts the normal MAUI/WinUI app.
- On Windows 10 the app runs normally but has no Widgets Board. The README says so.

---

## 9. Branding and visual assets

All artwork is original. No Supercars or Bathurst 1000 logos or trademarked marks are used.

- **Palette**: superseded by `docs/design-system` (sunny day on the Mountain: sky-blue `#0A4FC9`, dial-navy `#143A7A`, stopwatch-orange `#FF5F14`, dial-yellow `#FFC21F`; Poppins). Originally: sunset over the mountain, deep purple `#2B124C` → magenta `#B0185E` → racing orange `#FF6A00` → gold `#FFC21A`. Checkered black and white for accents. Gum-tree green `#2E6B3A` for the mountain.
- **App icon** (adaptive: foreground + background layers):
  - Foreground: a bold, simplified outline of the Mount Panorama circuit. Conrod Straight, the Chase, Skyline and the Esses should be recognisable, drawn as a thick white ribbon with a checkered-flag flick at the start/finish.
  - Background: the sunset gradient with a mountain silhouette.
  - Must read clearly at 48 px. Source SVGs live in `assets-src/` and are exported to `Resources/AppIcon/appicon.svg` and `appiconfg.svg`.
- **App background**:
  - A layered vector landscape: mountain ridgeline, the illuminated track ribbon climbing the hill, motion-blur speed streaks and the sunset sky.
  - A darker overlay keeps the countdown text readable (at least WCAG AA contrast).
  - Two crops: portrait (phone) and landscape (desktop).
- **Splash**: the circuit outline on the purple → magenta gradient.
- **Widget art**: a cropped, low-detail version of the background so text stays readable at small sizes. Also Windows widget icons and a screenshot for the widget picker.
- Asset creation: design the SVGs by hand or with an AI image tool, then vectorise them and check them at every target size (Android mipmaps, Windows scale-100…400 tiles).

---

## 10. Project configuration changes

- `.csproj`:
  - Set `<TargetFrameworks>` to `net10.0-android;net10.0-windows10.0.19041.0`. Remove the Apple conditions.
  - Delete `Platforms/iOS` and `Platforms/MacCatalyst`.
  - `ApplicationTitle`: "Bathurst Countdown".
  - `ApplicationId`: `au.bigriversoftware.bathurstcountdown`. This is also the Android package name.
  - `WindowsPackageType`: `MSIX`.
- **App identity (D10)**:

  | Setting | Value |
  |---------|-------|
  | Company / publisher display name | Big River Software |
  | Android package name (`ApplicationId`) | `au.bigriversoftware.bathurstcountdown` |
  | MSIX `Identity Name` | `BigRiverSoftware.BathurstCountdown` |
  | MSIX `Identity Publisher` | `CN=Big River Software` (must match the signing certificate's subject exactly) |
  | MSIX `PublisherDisplayName` | Big River Software |
  | Windows widget provider COM CLSID | `AF609A3B-CBBB-4A48-BE42-84FF9CF5F9B2` |
  | Windows widget definition id | `BathurstCountdown.Countdown` |
  | Android widget receiver name | `au.bigriversoftware.bathurstcountdown.CountdownWidgetProvider` |

  - Android `SupportedOSPlatformVersion`: `26.0`.
  - Remove `dotnet_bot.png`.
- NuGet packages: `CommunityToolkit.Mvvm`, `CommunityToolkit.Maui` (optional), `Xamarin.AndroidX.Work.Runtime` (WorkManager), and `Microsoft.Extensions.Http` for a typed `HttpClient`.
- Dependency injection in `MauiProgram`: `TimeProvider.System`, `IEventFeedClient`, `FeedCache`, `CountdownViewModel`, and a platform `IWidgetUpdater`.

---

## 11. Testing strategy

| Layer | Approach |
|-------|----------|
| Core logic | xUnit + `FakeTimeProvider`. Cover the state transitions, DST edge cases, multiple events, the "race just started" roll-over, empty or invalid feeds, and filter matching. |
| Feed builder | Snapshot tests against saved HTML fixtures from supercars.com, so a site change shows up as a failing test instead of a bad publish. Plus validator tests. |
| Feed client | `HttpMessageHandler` stubs for 200 / 304 / timeout / malformed responses and cache fallback. |
| App UI | Manual test matrix on an Android emulator (API 26, 34, 35) and Windows 11. Optional Appium smoke test later. |
| Widgets | Manual checklist: add, resize, reboot, airplane mode, time-zone change, the T−24h chronometer hand-over, and the post-race roll-over. |
| CI | `build.yml` builds both targets and runs the tests on `windows-latest`. `publish-feed.yml` builds, validates and publishes the feed. |

---

## 12. Delivery phases

### Phase 0 — Groundwork
- Move the project to `src/`, trim the target frameworks, remove the Apple platforms, set the IDs and names.
- Create `RaceCountdown.Core` and the test project; set up CI.
- **Spike S1**: find the most reliable way to get the Bathurst race start time from supercars.com (rendered HTML vs. the site's underlying JSON/API calls).
- **Spike S2**: prove that a packaged MAUI Windows app can host a COM widget provider in-process, and choose between D5 and its fallback.
- *Done when*: an empty app builds for both platforms in CI, and both spikes have written conclusions.

### Phase 1 — Core and feed
- Models, JSON schema, validator, `NextSessionSelector`, `CountdownCalculator`, `WidgetSnapshot`, and their tests.
- `EventFeedBuilder` + `overrides.json` + `publish-feed.yml`. Publish the first live `events.json` to GitHub Pages.
- *Done when*: the feed is live with the 2026 Bathurst 1000 race at 2026-10-11 11:30 Australia/Sydney, and Core has more than 90% line coverage.

### Phase 2 — App
- Feed client, cache, bundled snapshot, `CountdownViewModel`, `CountdownPage` with every state, refresh policy.
- *Done when*: the countdown is correct on both platforms, works offline from a cold start, and rolls over after a simulated race start.

### Phase 3 — Branding
- Icon, splash, backgrounds, widget art; light and dark theme polish.
- *Done when*: the assets look right at every density and scale factor and pass the contrast check.

### Phase 4 — Android widget
- Provider, layouts in both sizes, chronometer hand-over, WorkManager refresh, boot receiver.
- *Done when*: the widget checklist passes on API 26 and 35.

### Phase 5 — Windows widget
- COM provider, Adaptive Card templates, manifest entries, minute updates while active.
- *Done when*: the widget can be pinned from the Widgets Board on Windows 11, updates, and survives a reboot.

### Phase 6 — Release (sideloaded, D11)
- **Android**:
  - Create a release keystore (`bigriversoftware.keystore`) and keep it and its passwords **outside the repo**. Losing it means updates can't be installed over the existing app.
  - Build a signed APK: `dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk` with the signing properties.
  - Users must allow "Install unknown apps" for their browser or file manager.
- **Windows**:
  - Create a self-signed code-signing certificate with subject `CN=Big River Software`, and sign the MSIX with it.
  - Publish the `.cer` (public part only) next to the `.msix`.
  - Users import the `.cer` into *Local Machine → Trusted People* once, then double-click the `.msix` to install. The widget needs the packaged install, so an unpackaged exe is not offered.
- **GitHub Releases**: a `release.yml` workflow runs on a version tag, builds both packages, signs them using secrets stored in GitHub, and attaches the APK, MSIX, `.cer` and SHA-256 checksums to the release.
- Version from the tag: `ApplicationDisplayVersion` = `1.2.3`, `ApplicationVersion` = the build number.
- README install instructions for both platforms; privacy note (the app collects no personal data).
- *Done when*: a fresh Android device and a fresh Windows 11 PC can install from the GitHub Release by following only the README, and an upgrade installs over the previous version.
- *Later*: Microsoft Store and Google Play. The app id stays the same; the Store assigns its own publisher CN, which will replace `CN=Big River Software` in the Windows manifest.

### Later (not in v1)
- More events, series and tracks with a picker; followed-events settings.
- Notifications, e.g. "1 hour to green flag".
- Per-widget event choice (Android configure activity, Windows `IWidgetProvider2` customisation).
- Localisation.

---

## 13. Risks and mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| supercars.com markup or API changes | Feed goes stale | Parsing happens on the server with fixture tests; failures alert the maintainer; `overrides.json` allows manual fixes; the app keeps the last good feed. |
| Start time changes late (weather, TV schedule) | Countdown is wrong | Hourly refresh during race week; overrides can be published within minutes. |
| Scraping restricted by terms or robots.txt | Legal or ethical problem | Check in spike S1. If disallowed, keep the feed curated by hand (one event a year is easy to maintain) and use Wikipedia for dates. |
| MAUI and a COM widget provider in one exe cause trouble | Windows widget delayed | Spike S2 early; the fallback is a separate provider exe in the same MSIX. |
| Android background limits / OEM battery killers | Widget text goes stale | Chronometer ticks without wake-ups in the final 24h; inexact hourly alarms; tapping the widget always refreshes. |
| Time-zone or DST bugs | Countdown off by an hour | IANA zone ids, UTC instants, dedicated DST tests, and the feed validator cross-checks local time against UTC. |
| IANA zone lookup on Windows | Wrong offset | .NET 6+ with ICU supports `TimeZoneInfo.FindSystemTimeZoneById("Australia/Sydney")` on Windows. Covered by a test. |
| Trademark or branding concerns | Store rejection | Original artwork only; a "not affiliated with Supercars" disclaimer in the app and store listing. |

---

## 14. Resolved questions

| Question | Answer | Recorded as |
|----------|--------|-------------|
| Publisher identity | Big River Software, `au.bigriversoftware.bathurstcountdown` | D10, §10 |
| Distribution | Sideloaded to start with | D11, Phase 6 |
| Feed hosting | GitHub Pages; the repo will be public | D14 |
| Which race counts | Bathurst 1000 main race only | D12 |
| Between seasons | Show "TBA" | D13, §6 |
