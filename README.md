<p align="center">
  <img src="docs/icons/bathurst-icon-180.png" width="128" height="128" alt="Bathurst Countdown app icon: a stopwatch with the Mount Panorama circuit on its face">
</p>

<h1 align="center">Bathurst Countdown</h1>

<p align="center">
  <a href="https://github.com/BigRiverSoftware/RaceCountdown/actions/workflows/release.yml"><img src="https://github.com/BigRiverSoftware/RaceCountdown/actions/workflows/release.yml/badge.svg" alt="Release build"></a>
  <a href="https://github.com/BigRiverSoftware/RaceCountdown/actions/workflows/publish-feed.yml"><img src="https://github.com/BigRiverSoftware/RaceCountdown/actions/workflows/publish-feed.yml/badge.svg" alt="Publish feed"></a>
  <a href="https://github.com/BigRiverSoftware/RaceCountdown/releases/latest"><img src="https://img.shields.io/github/v/release/BigRiverSoftware/RaceCountdown?include_prereleases&sort=semver" alt="Latest release"></a>
  <a href="https://github.com/BigRiverSoftware/RaceCountdown/releases"><img src="https://img.shields.io/github/downloads/BigRiverSoftware/RaceCountdown/total" alt="Downloads"></a>
  <a href="LICENSE.txt"><img src="https://img.shields.io/github/license/BigRiverSoftware/RaceCountdown" alt="License"></a>
  <br>
  <a href="https://dotnet.microsoft.com/download/dotnet/10.0"><img src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white" alt=".NET 10"></a>
  <img src="https://img.shields.io/badge/platform-Android%20%7C%20Windows-0A4FC9" alt="Platforms: Android and Windows">
  <a href="https://github.com/BigRiverSoftware/RaceCountdown/commits"><img src="https://img.shields.io/github/last-commit/BigRiverSoftware/RaceCountdown" alt="Last commit"></a>
  <a href="https://github.com/BigRiverSoftware/RaceCountdown/issues"><img src="https://img.shields.io/github/issues/BigRiverSoftware/RaceCountdown" alt="Open issues"></a>
  <a href="https://github.com/BigRiverSoftware/RaceCountdown/pulls"><img src="https://img.shields.io/github/issues-pr/BigRiverSoftware/RaceCountdown" alt="Open pull requests"></a>
  <a href="https://github.com/BigRiverSoftware/RaceCountdown/stargazers"><img src="https://img.shields.io/github/stars/BigRiverSoftware/RaceCountdown?style=flat" alt="Stars"></a>
</p>

A .NET MAUI app by **Big River Software** for **Windows** and **Android** with one job: counting down to the start of the next **Bathurst 1000** Supercars race at **Mount Panorama, Bathurst**.

App id: `au.bigriversoftware.bathurstcountdown`

> **Status:** The app and both home-screen widgets are complete, and the race feed is live. Signed sideload releases are on GitHub Releases. Applications are pending to place this app into the app stores. See [docs/PROGRESS.md](docs/PROGRESS.md) for progress and [docs/PLAN.md](docs/PLAN.md) for the full plan.

## Features

- **Live countdown** to the green flag of the Bathurst 1000 main race in days, hours, minutes and seconds. Shows the start time in both your local time and Bathurst time.
- **"TBA" between seasons.** Until next year's start time is published, the app shows "TBA" and never counts down to a guessed date.
- **Updates itself.** Race times come from a public event feed built from public web sources. After a race starts, the app moves on to the next one without an app update.
- **Works offline.** The last downloaded schedule is cached, and the app ships with a built-in snapshot for first launch.
- **Home-screen widgets:**
  - **Android:** three widgets in the picker:
    - A resizable widget (4×2 down to 2×1) showing days, hours and minutes, ticking live during the final 24 hours.
    - A 2×2 widget showing the same.
    - A 1×1 widget that shows days to go, then hours:minutes on race day, then a live minutes:seconds countdown for the final 30 minutes.
  - **Windows 11:** a Widgets Board widget (small, medium and large) showing days, hours and minutes, built with Adaptive Cards.
- **Artwork:** a stopwatch icon with the Mount Panorama circuit on its face, over a sunny day on the Mountain: blue sky, green hills, a red-and-white kerb and orange race-timing accents. See the [design system](docs/design-system/README.md).
- **Ready for more events.** The data model supports multiple series, tracks and events worldwide. v1 shows only Bathurst.

## Platform support

| Platform | Minimum version | Widget |
|----------|-----------------|--------|
| Android | 8.0 (API 26) | Home-screen App Widget |
| Windows | 10 (1809, build 17763) | Windows 11 only (Widgets Board) |
| iOS / macOS | Not supported | — |

## Installing (sideload)

The app isn't in the app stores yet. Download the latest files from the [GitHub Releases](https://github.com/BigRiverSoftware/RaceCountdown/releases) page.

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
# Restore and build everything (the Windows target builds only on Windows)
dotnet build RaceCountdown.slnx

# Run on Windows
dotnet build src/RaceCountdown -f net10.0-windows10.0.19041.0 -t:Run

# Run on Android (emulator or connected device)
dotnet build src/RaceCountdown -f net10.0-android -t:Run

# Run the unit tests (core logic and the feed builder)
dotnet test tests/RaceCountdown.Core.Tests
dotnet test tests/EventFeedBuilder.Tests
```

`tools/dev/Register-WindowsApp.ps1` registers a local build as a packaged app, so you can try the Windows widget without Visual Studio. It uninstalls the package first, which also removes pinned widgets.

### Adding the widget
- **Android:** long-press the home screen → *Widgets* → *Bathurst Countdown* → drag it into place.
- **Windows 11:** open the Widgets Board (<kbd>Win</kbd>+<kbd>W</kbd>) → *+ Add widgets* → *Bathurst Countdown*. The app must be installed as a package: deploy from Visual Studio or install the MSIX.

### Changing the artwork
The icon and backgrounds are the PNG masters in `docs/icons` and `docs/backgrounds`. After changing them:

```powershell
python -m pip install pillow
python tools/dev/build_art.py        # copy the art into the app and regenerate the widget background
python tools/dev/check_contrast.py   # check the page text against WCAG AA
```

Then delete `src/RaceCountdown/obj/<config>/<tfm>/resizetizer` before the next build. See [assets-src/README.md](assets-src/README.md).

## How race data works

```
 public sources ──► EventFeedBuilder (GitHub Action, daily) ──► events.json (GitHub Pages)
 (supercars.com,        parse · merge overrides · validate              │
  overrides.json)                                                       ▼
                                                         app + widgets (cached on device)
```

- The app never scrapes websites. It downloads one small, validated JSON feed from <https://bigriversoftware.github.io/RaceCountdown/events.json>.
- The feed is rebuilt daily, and more often in race week. If any check fails, nothing is published and the previous feed stays live.
- If a start time is wrong or changes late, add a correction to [`feed/overrides.json`](feed/overrides.json). The next feed run publishes it.
- Times are stored as UTC instants together with the track's IANA time zone (`Australia/Sydney`), so daylight saving is handled correctly.

The feed schema and the rules for validating and publishing are in [docs/PLAN.md § 4–5](docs/PLAN.md#4-data-model-future-proofed-for-many-events) and [feed/README.md](feed/README.md).

## Releases

| Workflow | Trigger | Output |
|----------|---------|--------|
| [Release](.github/workflows/release.yml) | Push a `v1.2.3` or `v1.2.3.4` tag, or run by hand | APK and MSIX signed with the release keys, with `SHA256SUMS.txt`, on a GitHub Release |
| [Developer test build](.github/workflows/dev-release.yml) | Run by hand | Test-signed APK and MSIX on a GitHub pre-release |
| [Publish feed](.github/workflows/publish-feed.yml) | Daily schedule, or run by hand | `events.json` on GitHub Pages |

## Project structure

```
src/RaceCountdown/          MAUI app, plus Android and Windows widget implementations
src/RaceCountdown.Core/     Models, feed client, countdown logic (platform-neutral, unit-tested)
tests/                      xUnit tests for the core and the feed builder
tools/EventFeedBuilder/     Builds events.json from public sources (runs in CI)
tools/dev/                  Developer scripts: art build, contrast check, Windows package registration
feed/                       Feed JSON schema and manual overrides
docs/design-system/         Colour, type, spacing and radius tokens, and the rules for using them
docs/icons/, docs/backgrounds/   Icon and background masters
assets-src/                 Notes on how the artwork reaches the app, and font licences
docs/PLAN.md                Implementation plan
docs/PROGRESS.md            Progress by phase
```

## Roadmap

- [x] Phase 0: groundwork, trimming to Android and Windows, technical spikes
- [x] Phase 1: core logic and published event feed
- [x] Phase 2: countdown app
- [x] Phase 3: icon, splash and background artwork
- [x] Phase 4: Android widget
- [x] Phase 5: Windows 11 widget
- [ ] Phase 6: signed sideload releases (APK and MSIX) on GitHub Releases
- [ ] Later: Microsoft Store and Google Play
- [ ] Later: more tracks and series, notifications, per-widget event choice

## Disclaimer

Bathurst Countdown is an independent app by Big River Software. It is not affiliated with, endorsed by or connected to Supercars Australia, the Bathurst 1000 or Mount Panorama Circuit. All artwork is original. Race times come from publicly available information and may change; check official sources before planning travel.

## License

Released under the MIT License. See [LICENSE.txt](LICENSE.txt). The Poppins font is under the [SIL Open Font License](assets-src/fonts/Poppins-OFL.txt).
