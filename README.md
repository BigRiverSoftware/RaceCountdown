# Bathurst Countdown

A .NET MAUI app by **Big River Software** for **Windows** and **Android** with one job: counting down to the start of the next **Bathurst 1000** Supercars race at **Mount Panorama, Bathurst**.

App id: `au.bigriversoftware.bathurstcountdown`

> **Status:** Planning. See [docs/PLAN.md](docs/PLAN.md) for the full plan. Features below describe the target for v1.

## Features

- **Live countdown** to the green flag of the Bathurst 1000 main race in days, hours, minutes and seconds. Shows the start time in both your local time and Bathurst time.
- **"TBA" between seasons.** Until next year's start time is published, the app shows "TBA" and never counts down to a guessed date.
- **Updates itself.** Race times come from a public event feed built from public web sources. After a race starts, the app moves on to the next one without an app update.
- **Works offline.** The last downloaded schedule is cached, and the app ships with a built-in snapshot for first launch.
- **Home-screen widgets:**
  - **Android:** resizable App Widget. It ticks live during the final 24 hours.
  - **Windows 11:** a Widgets Board widget, built with Adaptive Cards.
- **Artwork** in sunset colours, based on the Mount Panorama circuit and mountain.
- **Ready for more events.** The data model supports multiple series, tracks and events worldwide. v1 shows only Bathurst.

## Platform support

| Platform | Minimum version | Widget |
|----------|-----------------|--------|
| Android | 8.0 (API 26) | Home-screen App Widget |
| Windows | 10 (1809, build 17763) | Windows 11 only (Widgets Board) |
| iOS / macOS | Not supported | — |

## Installing (sideload)

The app isn't in the app stores yet. Download the latest files from the [GitHub Releases](../../releases) page.

### Android
1. Download `BathurstCountdown-<version>.apk` on your phone.
2. Open it. If asked, allow your browser or file manager to **Install unknown apps**.
3. Tap **Install**. Later versions install over the top and keep your widgets.

### Windows
The app is an MSIX package signed by **Big River Software** with a self-signed certificate. Windows must trust that certificate once:

1. Download `BigRiverSoftware.cer` and `BathurstCountdown-<version>.msix`.
2. Trust the certificate (one time only). In an **administrator** PowerShell:
   ```powershell
   Import-Certificate -FilePath .\BigRiverSoftware.cer -CertStoreLocation Cert:\LocalMachine\TrustedPeople
   ```
   Or double-click the `.cer` → *Install Certificate* → *Local Machine* → *Trusted People*.
3. Double-click the `.msix` and choose **Install**.

Check each download against the `SHA256SUMS.txt` file published with the release.

## Building from source

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- The .NET MAUI workload: `dotnet workload install maui`
- Visual Studio 2026 with the **.NET Multi-platform App UI development** workload, or VS Code with the C# Dev Kit and .NET MAUI extensions
- For Android: the Android SDK, plus an emulator (API 26 or later) or a physical device
- For the Windows widget: Windows 11 with Developer Mode enabled, so the app can be deployed as a packaged MSIX

### Build and run

```powershell
# Restore and build everything
dotnet build RaceCountdown.slnx

# Run on Windows
dotnet build src/RaceCountdown -f net10.0-windows10.0.19041.0 -t:Run

# Run on Android (emulator or connected device)
dotnet build src/RaceCountdown -f net10.0-android -t:Run

# Run unit tests
dotnet test tests/RaceCountdown.Core.Tests
```

> Until Phase 0 of the plan moves the project, it lives at `RaceCountdown/` rather than `src/RaceCountdown/`.

### Adding the widget
- **Android:** long-press the home screen → *Widgets* → *Bathurst Countdown* → drag it into place.
- **Windows 11:** open the Widgets Board (<kbd>Win</kbd>+<kbd>W</kbd>) → *+ Add widgets* → *Bathurst Countdown*. The app must be installed as a package: deploy from Visual Studio or install the MSIX.

## How race data works

```
 public sources ──► EventFeedBuilder (GitHub Action, daily) ──► events.json (GitHub Pages)
 (supercars.com,        parse · merge overrides · validate              │
  Wikipedia,                                                            ▼
  overrides.json)                                        app + widgets (cached on device)
```

- The app never scrapes websites. It downloads one small, validated JSON feed.
- If a start time is wrong or changes late, add a correction to [`feed/overrides.json`](feed/overrides.json). The next feed run publishes it.
- Times are stored as UTC instants together with the track's IANA time zone (`Australia/Sydney`), so daylight saving is handled correctly.

The feed schema and the rules for validating and publishing are in [docs/PLAN.md § 4–5](docs/PLAN.md#4-data-model-future-proofed-for-many-events).

## Project structure

```
src/RaceCountdown/          MAUI app, plus Android and Windows widget implementations
src/RaceCountdown.Core/     Models, feed client, countdown logic (platform-neutral, unit-tested)
tests/                      xUnit tests
tools/EventFeedBuilder/     Builds events.json from public sources (runs in CI)
feed/                       Feed JSON schema and manual overrides
assets-src/                 Source artwork (SVG)
docs/PLAN.md                Implementation plan
```

## Roadmap

- [ ] Phase 0: groundwork, trimming to Android and Windows, technical spikes
- [ ] Phase 1: core logic and published event feed
- [ ] Phase 2: countdown app
- [ ] Phase 3: icon, splash and background artwork
- [ ] Phase 4: Android widget
- [ ] Phase 5: Windows 11 widget
- [ ] Phase 6: signed sideload releases (APK and MSIX) on GitHub Releases
- [ ] Later: Microsoft Store and Google Play
- [ ] Later: more tracks and series, notifications, per-widget event choice

## Disclaimer

Bathurst Countdown is an independent app by Big River Software. It is not affiliated with, endorsed by or connected to Supercars Australia, the Bathurst 1000 or Mount Panorama Circuit. All artwork is original. Race times come from publicly available information and may change; check official sources before planning travel.

## License

To be decided.
