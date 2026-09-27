# Spike S2 — Hosting a Windows 11 widget provider in the MAUI app

**Date:** 2026-09-27
**Result:** Keep D5: the widget provider runs inside the app's own exe. The API is available with no SDK changes. Registering a provider with the live Widgets Board still needs a hands-on test on Windows 11, which is the first task of Phase 5.

## Question
Can the packaged MAUI Windows app act as the Windows 11 widget provider itself, or does it need a separate provider exe (the D5 fallback)?

## Findings

1. **Packaging.** Only packaged (MSIX) apps can register as widget providers. Phase 0 already switched the app to `WindowsPackageType=MSIX` with the `BigRiverSoftware.BathurstCountdown` identity. The MSIX build succeeds.
2. **API availability.**
   - MAUI 10 brings in `Microsoft.WindowsAppSDK` **1.7.250909003**, found in `obj/project.assets.json`.
   - That package contains `Microsoft.Windows.Widgets.winmd` and the C# projection `Microsoft.Windows.Widgets.Projection.dll`. So `IWidgetProvider`, `WidgetManager` and related types are available to the app with no extra packages.
   - Widget customisation (`IWidgetProvider2`) needs SDK 1.4 or later, so 1.7 also covers the "later" per-widget event choice.
   - Microsoft's current tutorial uses SDK 2.3.1, but that is the tutorial's version, not a minimum for the widget API.
3. **Activation options** (widget provider manifest docs):
   - `CreateInstance`: out-of-process COM server identified by a `ClassId`. Microsoft recommends this. It is declared with `com:ExeServer`, and the process calls `CoRegisterClassObject` with an `IClassFactory`.
   - `ActivateApplication`: the host launches the exe with base64url-encoded JSON arguments. No COM class factory is needed. This is a lower-friction backup if hosting COM next to WinUI turns out awkward.
4. **Process model.**
   - The provider is activated separately from the app UI, and multiple providers from one app share one process.
   - When activated with `-RegisterProcessAsComServer`, the exe must register the factory and **not** start the MAUI window.
   - That needs a custom entry point: define `DISABLE_XAML_GENERATED_MAIN`, then have a `Program.Main` check the arguments before calling `Microsoft.UI.Xaml.Application.Start`.

## Decision
- **Primary (D5):** one exe.
  - Custom `Program.Main` under `Platforms/Windows`, with `CreateInstance` activation and CLSID `AF609A3B-CBBB-4A48-BE42-84FF9CF5F9B2` (plan §10).
  - In COM-server mode, run a message loop and exit when no widgets remain, as the Microsoft sample does.
- **Fallback A:** switch the manifest to `ActivateApplication`.
- **Fallback B:** a separate small provider console exe in the same MSIX.

## Not yet proven (carried into Phase 5, first task)
- Pinning the widget from the Widgets Board on a Windows 11 machine. This needs Developer Mode and an interactive session, so it can't be verified in CI.
- That `DISABLE_XAML_GENERATED_MAIN` and the MAUI single-project Windows build work together, with no duplicate `Main`.
