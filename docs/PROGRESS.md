# Progress — Phases 0 and 1 (paused)

Work stopped partway through Phase 0 on 2026-09-27. This note records what is done, what is broken, and what is next.

## Done
- Moved the app project from `RaceCountdown/` to `src/RaceCountdown/` (tracked files only).
- Removed the iOS and MacCatalyst platform folders and `dotnet_bot.png`.
- `src/RaceCountdown/RaceCountdown.csproj`:
  - Android + Windows target frameworks only.
  - Title "Bathurst Countdown", version 0.1.0.
  - `WindowsPackageType` MSIX, Android minimum API 26.
  - Placeholder icon and splash colour `#2B124C`.
  - Adds a reference to `..\RaceCountdown.Core`.
- `Package.appxmanifest`: identity `BigRiverSoftware.BathurstCountdown` / `CN=Big River Software`, plus the `internetClient` capability.
- `launchSettings.json`: `MsixPackage` profile.

## Known broken (the build will fail until fixed)
- `RaceCountdown.slnx` still points at `RaceCountdown/RaceCountdown.csproj`. Change it to `src/RaceCountdown/RaceCountdown.csproj`.
- The csproj references `src/RaceCountdown.Core`, which doesn't exist yet.
- `MainPage.xaml` still uses the template content, including the deleted `dotnet_bot.png` image. Replace it with a minimal placeholder page.
- The old `RaceCountdown/` folder still holds `bin/`, `obj/` and `RaceCountdown.csproj.user`. Visual Studio had them locked. Delete the folder once VS is closed.

## Spike S1 — findings (supercars.com data)
- `robots.txt` disallows `/api/`, `/account/`, `/auth/`, `/superview/videos/` and `/barcode/`. The calendar and event pages are allowed.
- The pages are Next.js App Router pages. Their data is embedded in `self.__next_f.push([1,"<json-string>"])` script blocks.
- **Parsing approach (verified with a probe script):**
  1. Regex-extract every push payload and JSON-decode each string.
  2. Concatenate the decoded strings, then split into lines.
  3. For each line of the form `<id>:<json>` where the JSON starts with `[` or `{`, parse it.
  4. Walk the parsed tree recursively. All rows parsed; none failed.
- **`/calendar`**: event objects have `slug`, `title`, `location`, `startDate` and `endDate`. Dates are ISO-8601 with an offset.
  - Example: `slug: "2026-bathurst-1000"`, `title: "2026 Repco Bathurst 1000"`, `startDate: "2026-10-08T06:00:00.000+11:00"`, `endDate: "2026-10-11T18:00:00.000+11:00"`.
- **`/events/<slug>`**: session objects have `name`, `type`, `startDate`, `endDate`, `durationLabel` and `series.name`.
  - Types: `Practice`, `Qualifying`, `Shootout`, `Warm Up`, `Race`, `On Track Activity`.
  - Support categories are sessions of the same event with a different `series.name`. Filter on `series.name == "Repco Supercars Championship"` and `type == "Race"`.
  - 2026 main race: `name: "Race 30"`, `startDate: "2026-10-11T11:30:00.000+11:00"`, `endDate: "2026-10-11T18:30:00.000+11:00"`, `durationLabel: "161 laps"`.
  - Use `endDate − startDate` for `estimatedDuration`.
- **Conclusion**: the feed builder only needs the calendar page plus one event page, and needs no HTML parser. Save both pages as test fixtures when building `EventFeedBuilder`.

## Spike S2 — partial findings (Windows widget)
- Only packaged apps can be widget providers. Microsoft's widget tutorial currently uses Windows App SDK **2.3.1** as its minimum version.
- **Next step**: after the first successful build, check which Windows App SDK version MAUI 10 brings in (`obj/project.assets.json`).
- Activation options, from the widget provider manifest docs:
  - `CreateInstance` (COM `ClassId`, recommended).
  - `ActivateApplication` (the app is launched with base64url JSON arguments). This could avoid hosting a COM server inside MAUI, so it's the alternative to evaluate if in-process COM is awkward.
- Docs: https://learn.microsoft.com/windows/apps/develop/widgets/implement-widget-provider-cs and https://learn.microsoft.com/windows/apps/develop/widgets/widget-provider-manifest

## Remaining Phase 0
- Fix the broken items above.
- Create `src/RaceCountdown.Core` (net10.0), `tests/RaceCountdown.Core.Tests` (xUnit + coverlet) and `tools/EventFeedBuilder`, and add them to the slnx.
- Add `.github/workflows/build.yml` (windows-latest; install the MAUI workload; build both targets; run the tests).
- Write up S1 and S2 in `docs/spikes/`.

## Phase 1 (not started)
- Models, validator, `NextSessionSelector`, `CountdownCalculator`, `WidgetSnapshot` and their tests.
- JSON schema, `overrides.json`, `EventFeedBuilder` with fixture tests, and `publish-feed.yml`.
- **Blocked on you**: publishing to GitHub Pages needs a GitHub remote (there is none yet) and Pages enabled on the repository.
