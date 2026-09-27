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
