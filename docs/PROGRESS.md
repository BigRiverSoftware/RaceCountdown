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

## Phase 1 — Core and feed: not started
- Models, validator, `NextSessionSelector`, `CountdownCalculator`, `WidgetSnapshot`, and tests covering more than 90% of Core.
- JSON schema, `overrides.json`, `EventFeedBuilder` with fixture tests (see S1), and `publish-feed.yml`.
- **Blocked on you**: publishing the feed needs a GitHub remote with Pages enabled.
