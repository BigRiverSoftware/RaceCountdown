# Decisions made during implementation (for review)

Choices made without asking, while working through the plan. Each one is easy to change; the note says where.

## Phase 2 — App

| # | Decision | Why | Where to change |
|---|----------|-----|-----------------|
| P2-1 | Feed client, cache, refresh policy, "feed store" and all page text formatting live in **Core**, not the app. The view model only ticks, calls Core, and binds. | They can be unit-tested without a device (plan §11 asks for `HttpMessageHandler` tests of the client). The Android widget worker (Phase 4) will reuse the same store. | `src/RaceCountdown.Core/Feed/`, `Countdown/CountdownDisplay.cs` |
| P2-2 | On load, the app uses **whichever of the cached feed and the bundled snapshot is newer** (by `generatedUtc`). The plan said "cache → bundled snapshot". | An app update can ship a newer snapshot than an old cache on the device. | `FeedStore.LoadAsync` |
| P2-3 | A downloaded feed that is **older** than the one showing is ignored (the ETag and success time are still recorded). | Protects against a CDN briefly serving an old copy. | `FeedStore.RefreshAsync` |
| P2-4 | Refresh timing: forced once at start-up, then 12-hourly, hourly when the race is **≤ 7 days away or live**, straight after the race **starts and again after it finishes**, and failed downloads retried at most every **15 minutes**. | Plan §5.3 plus a retry limit so an offline device doesn't try every second. "After it finishes" is what actually picks up the next event, because the selector keeps the race until its estimated end. | `RefreshPolicy` constants |
| P2-5 | The last-updated footer reads "Updated 28 Sept, 9:15 am", "Using built-in race data from …" before the first successful download, and "Offline — race data from …" when stale. | Plan §6 wording, made specific. | `CountdownDisplay.DescribeFooter` |
| P2-6 | On race day the days segment is hidden and the hours segment shows 00–24. While live, the segments are replaced by "RACE UNDERWAY" and "Green flag h:mm:ss ago". | Matches plan §6 display column. | `CountdownDisplay.Create`, `CountdownPage.xaml` |
| P2-7 | No tabular-figure font was added. Each countdown segment has a fixed width, so the layout does not move when digits change. | MAUI has no OpenType feature switch, and adding a monospaced font would clash with the Phase 3 look. Revisit in Phase 3 if the digits visibly wobble. | `CountdownPage.xaml` `Segment` style |
| P2-8 | "Not affiliated with Supercars or the Bathurst 1000." is shown in the page footer. | Plan §13 mitigation. | `CountdownPage.xaml` |
| P2-9 | HTTP: 20-second timeout; User-Agent `BathurstCountdown/<version> (+https://bigriversoftware.au)`. | Honest UA, as for the feed builder. | `MauiProgram.cs` |
| P2-10 | The bundled `Resources/Raw/events.snapshot.json` was generated **offline from the saved test pages** (`--source-dir tests/EventFeedBuilder.Tests/Fixtures`), not from a live fetch. It holds the real 2026 race time. | Reproducible. Regenerate before each release (to be added to the release checklist in Phase 6). | `src/RaceCountdown/Resources/Raw/` |
| P2-11 | Windows window opens at 960×640 (minimum 360×480). The wide layout starts at 720 px. | Fits a laptop screen; the minimum keeps the phone layout usable. | `App.xaml.cs`, `CountdownPage.xaml` |
| P2-12 | Added `tools/dev/Register-WindowsApp.ps1` to deploy the packaged app from the command line (like Visual Studio's F5). | `dotnet build -t:Run` can't start an MSIX app. Registering `bin` directly misses the snapshot and fonts. It is also needed to test the widget in Phase 5. | `tools/dev/` |

## Phase 3 — Branding

| # | Decision | Why | Where to change |
|---|----------|-----|-----------------|
| P3-1 | All artwork was drawn by hand as SVG. There is no AI-generated or raster art, and no separate export step (MAUI rasterises the SVGs at build time). The final SVGs live in `Resources/`; `assets-src/` holds only the master circuit shape and a README. | Keeps everything editable and diff-able; the plan's "export" step is not needed for SVG. | `assets-src/README.md` |
| P3-2 | The circuit is **stylised**, not traced from a map. It keeps the recognisable parts: Pit Straight, Hell Corner, the diagonal Mountain Straight, the twisty top, and Conrod Straight down the side. | Avoids copying any official track map or logo. Worth a look from someone who knows the track. | `assets-src/circuit.svg` |
| P3-3 | Readability: the art keeps a **light overlay** (25–35%) and the countdown sits on a **translucent rounded card**. Together they give at least 62% darkening behind all text. `tools/dev/check_contrast.py` checks every text style against the brightest colour in the art (conservative) and all pass WCAG AA. | A 62% overlay across the whole page made the art too dull. | `CountdownPage.xaml`, `tools/dev/check_contrast.py` |
| P3-4 | **Light and dark themes both use the same dusk artwork.** The page does not change with the system theme. | The design is built around the sunset art and white text; a light variant would need a second set of art and would weaken the look. The Android status bar uses the palette's darkest purple in both. | `CountdownPage.xaml`, `Platforms/Android/Resources/values/colors.xml` |
| P3-5 | Phone layout: countdown segments are 62 dp wide with 40 pt digits, so four fit on a 320–360 dp screen. The wide layout (≥ 720 px) goes back to 150 px / 84 pt. | The first version clipped the seconds segment on a 360 dp phone. | `CountdownPage.xaml` |
| P3-6 | The template's `Primary`/`Tertiary` colours were remapped to the palette (magenta, deep purple) so default controls match. The rest of the template `Styles.xaml` is unchanged. | Minimal change; only one page exists. | `Resources/Styles/Colors.xaml` |
| P3-7 | Background PNG sizes: base 360×640 / 640×360, so the largest generated file is about 1440 px (4x). The widget art is 320×160 base. | Keeps the APK and MSIX small; the art is soft gradients that upscale well. | `RaceCountdown.csproj` |

## Phase 4 — Android widget

| # | Decision | Why | Where to change |
|---|----------|-----|-----------------|
| P4-1 | **One resizable widget** (placed at 4×2, resizable down to 2×1) rather than separate small and medium widgets in the picker. Android 12+ picks the layout by size (responsive `RemoteViews`); older versions pick it from the widget's reported width (< 180 dp = compact). | Plan §8.2 lists both sizes and "resizable"; one entry keeps the picker tidy. | `countdown_widget_info.xml`, `CountdownWidgetViews.cs` |
| P4-2 | The widget shows **days + hours** (`WidgetSnapshot.ShortDetail`, new) and never minutes, because it only redraws when the hours change. The compact size shows just the days ("13 / DAYS TO GO"). | Minute-accurate text would need a redraw every minute; plan §8.2 says hourly. | `WidgetSnapshot`, `WidgetSchedule` |
| P4-3 | Redraws are driven by **one inexact, non-waking `RTC` alarm** for all widget instances, set to the exact moment the hours shown next change (e.g. 13:30:01 for an 11:30 start), the phase change, every 30 minutes while live, and every 6 hours while TBA. | No exact-alarm permission; no battery cost while the screen is off. | `WidgetSchedule`, `CountdownWidgetProvider.ScheduleRedraw` |
| P4-4 | While **live**, the widget shows "RACE UNDERWAY / The green flag has dropped" rather than elapsed time. | Elapsed time would be up to 30 minutes stale between redraws. | `WidgetSnapshot` (`ShortDetail`) |
| P4-5 | Feed refresh: a **WorkManager periodic job every 12 h** (network required) while a widget exists, plus a **one-off job** whenever a widget redraw finds `RefreshPolicy` due (so hourly in race week and straight after the start/finish). Failed runs are not retried by WorkManager; the policy's 15-minute limit handles retries. | Reuses the app's refresh rules instead of a second set. | `FeedRefreshWorker`, `CountdownWidgetProvider.RedrawAsync` |
| P4-6 | Besides boot, the system receiver also redraws on **time change, time-zone change and app update** (`MY_PACKAGE_REPLACED`). | The chronometer base and alarm times are computed from the wall clock; an app update clears nothing but should show new code at once. | `WidgetSystemReceiver` |
| P4-7 | **WorkManager 2.10.3**, not the latest 2.11.x. | 2.11 pulls AndroidX Lifecycle 2.11, which conflicts with the 2.9.2 that MAUI 10 uses (NU1608 warnings, possible runtime mismatch). Revisit when MAUI moves on. | `RaceCountdown.csproj` |
| P4-8 | The widget picker preview is the widget artwork with an empty text layout; no fake sample countdown is shown. | A hard-coded "13 days" preview would be misleading most of the year. | `countdown_widget_info.xml` |
